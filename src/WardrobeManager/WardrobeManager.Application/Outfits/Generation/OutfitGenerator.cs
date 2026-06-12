using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Generation;

public sealed class OutfitGenerator(
    IClothingRepository clothingRepository,
    IUserRepository userRepository,
    IOutfitFeedbackRepository feedbackRepository,
    IUserEvaluatorWeightsRepository weightsRepository,
    IEnumerable<IOutfitEvaluator> evaluators,
    ILogger<OutfitGenerator> logger) : IOutfitGenerator
{
    private const double DefaultMlWeight = 0.15;   // baseline vector-similarity weight, always applied
    private const double VetoThreshold = -0.99;    // an evaluator score at/below this excludes the candidate
    private const int CandidatesPerSlot = 30;      // how many similar items to retrieve and score per slot
    private const int ShownPerSlot = 4;            // best + 3 alternatives surfaced to the client

    public async Task<AiGeneratedOutfitDto> GenerateAiOutfitAsync(
        Guid userId, Guid startItemId, OutfitGenerationOptions options, CancellationToken ct = default)
    {
        var startItem = await clothingRepository.GetByIdAsync(startItemId, ct);
        if (startItem == null)
        {
            throw new KeyNotFoundException("Start item not found.");
        }
        if (startItem.Embedding == null)
            throw new InvalidOperationException("Start item has no embedding vector.");

        var context = await BuildContextAsync(userId, startItem, options, ct);

        var generationId = Guid.NewGuid();
        var selected = new List<SimilarItemDto>
        {
            new() { Id = startItem.Id, Name = startItem.Name, ProcessedImageUrl = startItem.ProcessedImageUrl, SimilarityScore = 1.0 }
        };
        var recommendations = new List<OutfitRecommendationDto>();
        var impressions = new List<OutfitFeedback>();
        var isValid = true;

        foreach (var type in GetNeededTypes(startItem.Type, context))
        {
            var slot = await ScoreSlotAsync(userId, startItem, type, context, generationId, ct);
            recommendations.Add(slot.Recommendation);
            impressions.AddRange(slot.Impressions);

            if (slot.BestItem != null)
            {
                selected.Add(slot.BestCandidate!);
                context.SelectedItems.Add(slot.BestItem); // next slot scores against the outfit so far
                if (slot.BestCandidate!.SimilarityScore < options.Threshold) isValid = false;
            }
            else
            {
                isValid = false;
            }
        }

        // The seed's own slot (the top): real comparable score + swappable alternatives, shown first.
        var seedSlot = await BuildSeedSlotAsync(userId, startItem, context, generationId, ct);
        recommendations.Insert(0, seedSlot.Recommendation);
        impressions.AddRange(seedSlot.Impressions);

        await LogImpressionsAsync(impressions, ct);

        return new AiGeneratedOutfitDto
        {
            GenerationId = generationId,
            Name = $"{(options.Style ?? "Custom")} Look with {startItem.Name}",
            SelectedItems = selected,
            RecommendationsPerType = recommendations,
            IsValid = isValid,
            Warnings = BuildConstraintWarnings(context)
        };
    }

    // Per-slot notes when the FINAL selection couldn't honor a requested constraint (the generator
    // degrades gracefully rather than leaving a slot empty, so we tell the user what was missing).
    private static IReadOnlyList<string> BuildConstraintWarnings(OutfitGenerationContext context)
    {
        var warnings = new List<string>();
        foreach (var item in context.SelectedItems)
        {
            if (!context.GarmentConstraints.TryGetValue(item.Type, out var spec)) continue;
            var label = SlotLabel(item.Type);

            if (!string.IsNullOrWhiteSpace(spec.SubType) &&
                !string.Equals(item.SubType, spec.SubType, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"No {spec.SubType} in your wardrobe — used another {label}.");
            }

            if (spec.AvoidColors.Count > 0 && spec.AvoidColors.Any(a => ColorFamily.ColorsMatch(item.Color, a)))
            {
                warnings.Add($"Couldn't avoid {Join(spec.AvoidColors)} for the {label} — your wardrobe had no other option.");
            }
            else if (spec.DesiredColors.Count > 0 && !spec.DesiredColors.Any(d => ColorFamily.ColorsMatch(item.Color, d)))
            {
                warnings.Add($"No {Join(spec.DesiredColors)} {label} in your wardrobe — used the closest match.");
            }
        }
        return warnings.Distinct().ToList();
    }

    private static string Join(IReadOnlyList<string> values) => string.Join(" or ", values);

    private static string SlotLabel(ClothingType type) => type switch
    {
        ClothingType.Top => "top",
        ClothingType.Bottom => "bottoms",
        ClothingType.Shoes => "shoes",
        ClothingType.Outerwear => "outerwear",
        ClothingType.Accessory => "accessory",
        _ => "item"
    };

    // Assembles all run state (user prefs, weather, learned weights, constraints) into one context.
    private async Task<OutfitGenerationContext> BuildContextAsync(
        Guid userId, ClothingItem startItem, OutfitGenerationOptions options, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);

        // Soft favorite-color preference — only when the prompt gave no explicit colors.
        IReadOnlyList<string> preferredColors =
            options.DesiredColors.Count == 0 && user?.FavoriteColors is { Count: > 0 } favs
                ? favs
                : new List<string>();

        var wearRecency = await clothingRepository.GetWearRecencyAsync(userId, ct);
        var learnedWeights = await weightsRepository.GetByUserIdAsync(userId, ct);

        return new OutfitGenerationContext
        {
            Weather = options.Weather,
            // No explicit style from the prompt -> follow the seed item's own style so the rest of
            // the outfit stays coherent (e.g. a casual tee won't pull in formal shoes).
            TargetStyle = DeriveTargetStyle(options.Style, startItem),
            DesiredColors = options.DesiredColors,
            AvoidColors = options.AvoidColors,
            PreferredColors = preferredColors,
            Occasion = options.Occasion,
            Formality = options.Formality,
            TargetGender = NormalizeGender(startItem.Gender),
            TemperatureHint = options.TemperatureHint,
            OuterwearMode = user?.OuterwearMode,
            OuterwearTempThreshold = user?.OuterwearTempThreshold ?? 23,
            RequestedTypes = options.RequestedTypes,
            GarmentConstraints = options.GarmentConstraints,
            WearRecency = wearRecency,
            SelectedItems = { startItem },
            LearnedWeights = learnedWeights?.Weights,
            LearnedMlWeight = learnedWeights?.MlWeight
        };
    }

    // Retrieves, constrains, scores and ranks candidates for a single clothing slot.
    private async Task<SlotResult> ScoreSlotAsync(
        Guid userId, ClothingItem startItem, ClothingType type,
        OutfitGenerationContext context, Guid generationId, CancellationToken ct)
    {
        // Anchor retrieval to the whole outfit so far (cohesion), not just the seed.
        var anchor = CentroidEmbedding(context.SelectedItems);
        if (anchor == null)
        {
            anchor = startItem.Embedding!;
        }

        var similarItems = await clothingRepository.GetSimilarItemsAsync(
            userId, anchor, type: type, limit: CandidatesPerSlot, threshold: null, gender: context.TargetGender, ct: ct);

        if (context.GarmentConstraints.TryGetValue(type, out var spec))
            similarItems = ApplyGarmentSpec(similarItems, spec);

        var componentsById = new Dictionary<Guid, Dictionary<string, double>>();
        var scored = new List<SimilarItemDto>();
        foreach (var (candidate, similarity) in similarItems)
        {
            if (candidate.Id == startItem.Id) continue;

            var (score, components) = ScoreCandidate(candidate, similarity, context);
            if (score < 0) continue; // vetoed or heavily penalized

            scored.Add(new SimilarItemDto
            {
                Id = candidate.Id,
                Name = candidate.Name,
                ProcessedImageUrl = candidate.ProcessedImageUrl,
                SimilarityScore = score
            });
            componentsById[candidate.Id] = components;
        }

        var sorted = scored.OrderByDescending(x => x.SimilarityScore).ToList();
        var top = sorted.Take(ShownPerSlot).ToList();

        // Log every shown candidate with its scoring features for later training.
        var impressions = new List<OutfitFeedback>(top.Count);
        for (var rank = 0; rank < top.Count; rank++)
        {
            var c = top[rank];
            impressions.Add(new OutfitFeedback
            {
                UserId = userId,
                GenerationId = generationId,
                ClothingItemId = c.Id,
                SlotType = type,
                Rank = rank,
                FinalScore = c.SimilarityScore,
                EvaluatorScores = componentsById.GetValueOrDefault(c.Id) ?? new Dictionary<string, double>()
            });
        }

        var bestCandidate = sorted.FirstOrDefault();
        var bestItem = bestCandidate != null
            ? similarItems.First(x => x.Item.Id == bestCandidate.Id).Item
            : null;

        return new SlotResult(
            new OutfitRecommendationDto { Type = type, TopCandidates = top },
            bestCandidate, bestItem, impressions);
    }

    private async Task<(OutfitRecommendationDto Recommendation, IReadOnlyList<OutfitFeedback> Impressions)>
        BuildSeedSlotAsync(Guid userId, ClothingItem startItem, OutfitGenerationContext context, Guid generationId, CancellationToken ct)
    {
        var type = startItem.Type;
        var similarItems = await clothingRepository.GetSimilarItemsAsync(
            userId, startItem.Embedding!, type: type, limit: CandidatesPerSlot, threshold: null, gender: context.TargetGender, ct: ct);

        // Filter alternatives by this slot's spec (color/sub-type) so they stay consistent with the
        // request; re-add the seed afterwards since it always stays the slot's selected piece.
        if (context.GarmentConstraints.TryGetValue(type, out var spec))
            similarItems = ApplyGarmentSpec(similarItems, spec);

        // Make sure the seed itself is in the pool (it's the slot's selected piece).
        if (similarItems.All(x => x.Item.Id != startItem.Id))
            similarItems = similarItems.Prepend((startItem, 1.0)).ToList();

        var componentsById = new Dictionary<Guid, Dictionary<string, double>>();
        var scored = new List<SimilarItemDto>();
        foreach (var (candidate, similarity) in similarItems)
        {
            var (score, components) = ScoreCandidate(candidate, similarity, context);
            // Keep the seed even if vetoed -> it stays the selected piece of its slot.
            if (score < 0 && candidate.Id != startItem.Id) continue;

            scored.Add(new SimilarItemDto
            {
                Id = candidate.Id,
                Name = candidate.Name,
                ProcessedImageUrl = candidate.ProcessedImageUrl,
                // Never surface a negative % for the kept seed (defensive; the selector already
                // filters context-vetoed items out of seed selection).
                SimilarityScore = Math.Max(0, score)
            });
            componentsById[candidate.Id] = components;
        }

        var top = scored.OrderByDescending(x => x.SimilarityScore).Take(ShownPerSlot).ToList();

        // Guarantee the seed is present so the UI identifies it as the slot's selected piece.
        if (top.All(c => c.Id != startItem.Id))
        {
            var seedDto = scored.First(c => c.Id == startItem.Id);
            top = top.Take(ShownPerSlot - 1).Prepend(seedDto).ToList();
        }

        var impressions = new List<OutfitFeedback>(top.Count);
        for (var rank = 0; rank < top.Count; rank++)
        {
            var c = top[rank];
            impressions.Add(new OutfitFeedback
            {
                UserId = userId,
                GenerationId = generationId,
                ClothingItemId = c.Id,
                SlotType = type,
                Rank = rank,
                FinalScore = c.SimilarityScore,
                EvaluatorScores = componentsById.GetValueOrDefault(c.Id) ?? new Dictionary<string, double>()
            });
        }

        return (new OutfitRecommendationDto { Type = type, TopCandidates = top }, impressions);
    }

    // Best-effort: feedback logging must never break generation.
    private async Task LogImpressionsAsync(IReadOnlyList<OutfitFeedback> impressions, CancellationToken ct)
    {
        if (impressions.Count == 0) return;
        try { await feedbackRepository.AddImpressionsAsync(impressions, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to log {Count} outfit impressions.", impressions.Count);
        }
    }

    // Weighted score plus the per-evaluator normalized scores (the training features).
    private (double Score, Dictionary<string, double> Components) ScoreCandidate(
        ClothingItem item, double mlSimilarity, OutfitGenerationContext context)
    {
        var components = new Dictionary<string, double>();
        double mlWeight;
        if (context.LearnedMlWeight.HasValue)
        {
            mlWeight = context.LearnedMlWeight.Value;
        }
        else
        {
            mlWeight = DefaultMlWeight;
        }

        var finalScore = mlSimilarity * mlWeight;
        var totalWeight = mlWeight;
        components["MlSimilarity"] = mlSimilarity;

        foreach (var evaluator in evaluators)
        {
            var evalScore = evaluator.Evaluate(item, context);
            if (evalScore is null) continue; // abstain -> excluded from the average entirely
            if (evalScore.Value <= VetoThreshold) return (-1.0, components); // hard veto

            var normalized = (evalScore.Value + 1.0) / 2.0; // [-1,1] -> [0,1]
            var weight = WeightFor(evaluator, context);
            finalScore += normalized * weight;
            totalWeight += weight;
            components[evaluator.Name] = normalized;
        }

        if (totalWeight > 0) finalScore /= totalWeight;
        return (finalScore, components);
    }

    private static List<(ClothingItem Item, double Similarity)> ApplyGarmentSpec(
        List<(ClothingItem Item, double Similarity)> items, GarmentSpec spec)
    {
        if (!string.IsNullOrWhiteSpace(spec.SubType))
        {
            var bySubType = items
                .Where(x => string.Equals(x.Item.SubType, spec.SubType, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (bySubType.Count > 0) items = bySubType;
        }

        if (spec.AvoidColors.Count > 0)
        {
            var kept = items
                .Where(x => !spec.AvoidColors.Any(a => ColorFamily.ColorsMatch(x.Item.Color, a)))
                .ToList();
            if (kept.Count > 0) items = kept;
        }

        if (spec.DesiredColors.Count > 0)
        {
            var matching = items
                .Where(x => spec.DesiredColors.Any(d => ColorFamily.ColorsMatch(x.Item.Color, d)))
                .ToList();
            if (matching.Count > 0) items = matching;
        }

        return items;
    }

    // Learned weight for this evaluator if the user has one, otherwise the evaluator's default.
    private static double WeightFor(IOutfitEvaluator evaluator, OutfitGenerationContext context) =>
        context.LearnedWeights != null && context.LearnedWeights.TryGetValue(evaluator.Name, out var w)
            ? w
            : evaluator.Weight;

    // Unisex/unknown seed imposes no gender constraint; otherwise the outfit is locked to it.
    private static string? NormalizeGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender)) return null;
        return gender.Equals("Unisex", StringComparison.OrdinalIgnoreCase) ? null : gender;
    }

    // The prompt's style wins; otherwise fall back to the seed item's own style (its first Usage
    // tag) so the whole outfit follows the vibe of the piece it's built around.
    private static string? DeriveTargetStyle(string? explicitStyle, ClothingItem startItem)
    {
        if (!string.IsNullOrWhiteSpace(explicitStyle)) return explicitStyle;
        if (string.IsNullOrWhiteSpace(startItem.Usage)) return null;
        return startItem.Usage.Split(',')[0].Trim();
    }

    // Element-wise mean of the selected items' embeddings, L2-normalized.
    private static float[]? CentroidEmbedding(IReadOnlyCollection<ClothingItem> items)
    {
        var vectors = items.Where(i => i.Embedding != null).Select(i => i.Embedding!).ToList();
        if (vectors.Count == 0) return null;

        var dim = vectors[0].Length;
        var sum = new double[dim];
        foreach (var v in vectors)
            for (var i = 0; i < dim && i < v.Length; i++) sum[i] += v[i];

        var mean = new float[dim];
        double norm = 0;
        for (var i = 0; i < dim; i++)
        {
            mean[i] = (float)(sum[i] / vectors.Count);
            norm += mean[i] * (double)mean[i];
        }

        norm = Math.Sqrt(norm);
        if (norm > 1e-6)
            for (var i = 0; i < dim; i++) mean[i] = (float)(mean[i] / norm);

        return mean;
    }

    private static List<ClothingType> GetNeededTypes(ClothingType startType, OutfitGenerationContext context)
    {
        var needed = new List<ClothingType>();
        void Add(ClothingType t) { if (startType != t && !needed.Contains(t)) needed.Add(t); }

        // Default basic outfit.
        Add(ClothingType.Top);
        Add(ClothingType.Bottom);
        Add(ClothingType.Shoes);

        // Outerwear: user policy first; "auto" falls back to live weather, then the temperature hint.
        bool wantOuterwear;
        switch (context.OuterwearMode?.ToLowerInvariant())
        {
            case "always": wantOuterwear = true; break;
            case "never": wantOuterwear = false; break;
            default: // "auto" / null
                if (context.Weather != null)
                {
                    wantOuterwear = context.Weather.Temperature <= context.OuterwearTempThreshold;
                }
                else
                {
                    wantOuterwear = context.TemperatureHint?.ToLowerInvariant() switch
                    {
                        "hot" or "warm" => false,
                        "cold" or "cool" => true,
                        _ => true // no info -> include to be safe
                    };
                }
                break;
        }
        if (wantOuterwear) Add(ClothingType.Outerwear);

        // Honor explicitly requested types, then always offer an accessory.
        foreach (var t in context.RequestedTypes) Add(t);
        Add(ClothingType.Accessory);

        return needed;
    }

    private sealed record SlotResult(
        OutfitRecommendationDto Recommendation,
        SimilarItemDto? BestCandidate,
        ClothingItem? BestItem,
        IReadOnlyList<OutfitFeedback> Impressions);
}
