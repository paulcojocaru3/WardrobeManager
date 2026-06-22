using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Generation;

// shared mechanics for every generation strategy: context building, candidate retrieval (Stage 1
public abstract class OutfitGeneratorBase(
    IClothingRepository clothingRepository,
    IUserRepository userRepository,
    IOutfitFeedbackRepository feedbackRepository,
    IItemPairScoreRepository pairScoreRepository,
    IUserLearningProfileRepository learningProfileRepository,
    IEnumerable<IOutfitEvaluator> evaluators,
    IGarmentFeasibility feasibility,
    ILogger logger) : IOutfitGenerator
{
    protected const double MlWeight = 0.15;     // baseline vector-similarity weight, always applied
    protected const int CandidatesPerSlot = 30; // similar items to retrieve and score per slot
    protected const int ShownPerSlot = 5;       // best + up to 4 alternatives surfaced to the client

    protected static readonly IReadOnlySet<ClothingType> EssentialTypes =
        new HashSet<ClothingType> { ClothingType.Top, ClothingType.Bottom, ClothingType.Shoes };

    protected IClothingRepository ClothingRepository => clothingRepository;
    protected IGarmentFeasibility Feasibility => feasibility;

    public async Task<AiGeneratedOutfitDto> GenerateAiOutfitAsync(
        Guid userId, Guid startItemId, OutfitGenerationOptions options, CancellationToken ct = default)
    {
        var startItem = await clothingRepository.GetByIdAsync(startItemId, ct);
        if (startItem == null)
        {
            throw new KeyNotFoundException("Start item not found.");
        }
        if (startItem.Embedding == null)
        {
            throw new InvalidOperationException("Start item has no embedding vector.");
        }

        var context = await BuildContextAsync(userId, startItem, options, ct);
        var generationId = Guid.NewGuid();
        var neededTypes = GetNeededTypes(startItem.Type, context);

        // strategy-specific: fill the non-seed slots. Leaves context.SelectedItems = the final outfit.
        var plan = await PlanSlotsAsync(userId, startItem, context, generationId, neededTypes, options, ct);

        // the seed's own slot (real comparable score + swappable alternatives), shown first.
        var seedSlot = await BuildSeedSlotAsync(userId, startItem, context, generationId, ct);
        var recommendations = new List<OutfitRecommendationDto> { seedSlot.Recommendation };
        recommendations.AddRange(plan.Recommendations);

        var impressions = new List<OutfitFeedback>(seedSlot.Impressions);
        impressions.AddRange(plan.Impressions);
        await LogImpressionsAsync(impressions, ct);

        var warnings = new List<string>(plan.Warnings);
        warnings.AddRange(BuildConstraintWarnings(context));

        return new AiGeneratedOutfitDto
        {
            GenerationId = generationId,
            Name = $"{(options.Style ?? "Custom")} Look with {startItem.Name}",
            SelectedItems = plan.Selected,
            RecommendationsPerType = recommendations,
            IsValid = plan.IsValid,
            Warnings = warnings.Distinct().ToList(),
            Candidates = plan.Candidates
        };
    }

    // the strategy hook. Returns the selected pieces (incl. the seed), the per-slot recommendations
    protected abstract Task<SlotPlan> PlanSlotsAsync(
        Guid userId, ClothingItem startItem, OutfitGenerationContext context, Guid generationId,
        IReadOnlyList<ClothingType> neededTypes, OutfitGenerationOptions options, CancellationToken ct);

    // ----- Stage 1: retrieval + feasibility -------------------------------------------------------

    // retrieves candidates for a slot anchored on the given embedding. For essential slots, retries
    protected async Task<List<(ClothingItem Item, double Similarity)>> RetrieveAsync(
        Guid userId, float[] anchor, ClothingType type, OutfitGenerationContext context, CancellationToken ct)
    {
        var items = await clothingRepository.GetSimilarItemsAsync(
            userId, anchor, type: type, limit: CandidatesPerSlot, threshold: null, gender: context.TargetGender, ct: ct);

        if (items.Count == 0 && context.TargetGender != null && EssentialTypes.Contains(type))
        {
            items = await clothingRepository.GetSimilarItemsAsync(
                userId, anchor, type: type, limit: CandidatesPerSlot, threshold: null, gender: null, ct: ct);
        }

        if (context.ExcludedItemIds.Count > 0)
        {
            var kept = items.Where(x => !context.ExcludedItemIds.Contains(x.Item.Id)).ToList();
            items = kept;
        }

        return items;
    }

    // the slot's feasible pool plus the relaxations it took. Guarantees the completeness invariant:
    protected SlotPool BuildSlotPool(
        ClothingType type, List<(ClothingItem Item, double Similarity)> raw, OutfitGenerationContext context)
    {
        var pool = feasibility.FilterWithRelaxation(raw, context);

        if (pool.Count == 0 && raw.Count > 0 && EssentialTypes.Contains(type))
        {
            var allKinds = (IReadOnlySet<ConstraintKind>)Enum.GetValues<ConstraintKind>().ToHashSet();
            pool = raw
                .OrderByDescending(r => r.Similarity)
                .Select(r => new FeasibleCandidate(r.Item, r.Similarity, allKinds))
                .ToList();
        }

        var relaxed = pool.SelectMany(c => c.Relaxed).ToHashSet();
        return new SlotPool(type, pool, relaxed, IsEmptyWardrobeSlot: raw.Count == 0);
    }

    // ----- Stage 2: soft scoring ------------------------------------------------------------------

    // multiplicative score based on the raw CLIP similarity and the soft evaluator multipliers,
    protected double ScoreItemSoft(ClothingItem item, double mlSimilarity, OutfitGenerationContext context)
    {
        // start with the ML baseline similarity.
        double finalScore = Math.Max(0.01, mlSimilarity);

        foreach (var evaluator in evaluators)
        {
            double multiplier = evaluator.Evaluate(item, context);
            // ensure we don't accidentally drop below a hard minimum due to precision
            finalScore *= Math.Max(0.01, multiplier);
        }

        return finalScore;
    }

    // ----- Seed slot (shared) ---------------------------------------------------------------------

    protected async Task<(OutfitRecommendationDto Recommendation, IReadOnlyList<OutfitFeedback> Impressions)>
        BuildSeedSlotAsync(Guid userId, ClothingItem startItem, OutfitGenerationContext context, Guid generationId, CancellationToken ct)
    {
        var type = startItem.Type;
        var raw = await clothingRepository.GetSimilarItemsAsync(
            userId, startItem.Embedding!, type: type, limit: CandidatesPerSlot, threshold: null, gender: context.TargetGender, ct: ct);

        var pool = feasibility.FilterWithRelaxation(raw, context).Select(c => (c.Item, c.Similarity)).ToList();

        // make sure the seed itself is in the pool (it's the slot's selected piece).
        if (pool.All(x => x.Item.Id != startItem.Id))
            pool = pool.Prepend((startItem, 1.0)).ToList();

        var scored = new List<SimilarItemDto>();
        foreach (var (candidate, similarity) in pool)
        {
            var score = ScoreItemSoft(candidate, similarity, context);
            scored.Add(new SimilarItemDto
            {
                Id = candidate.Id,
                Name = candidate.Name,
                ProcessedImageUrl = candidate.ProcessedImageUrl,
                SimilarityScore = Math.Max(0, score)
            });
        }

        // --- NEW NORMALIZATION LOGIC ---
        var maxScore = scored.Count > 0 ? scored.Max(x => x.SimilarityScore) : 1.0;
        double scaleFactor = Math.Max(1.0, maxScore);

        for (int i = 0; i < scored.Count; i++)
        {
            var item = scored[i];
            scored[i] = new SimilarItemDto
            {
                Id = item.Id,
                Name = item.Name,
                ProcessedImageUrl = item.ProcessedImageUrl,
                SimilarityScore = item.SimilarityScore / scaleFactor
            };
        }

        var top = scored.OrderByDescending(x => x.SimilarityScore).Take(ShownPerSlot).ToList();

        // guarantee the seed is present so the UI identifies it as the slot's selected piece.
        if (top.All(c => c.Id != startItem.Id))
        {
            var seedDto = scored.First(c => c.Id == startItem.Id);
            top = top.Take(ShownPerSlot - 1).Prepend(seedDto).ToList();
        }

        var impressions = BuildImpressions(userId, generationId, type, top, context);
        return (new OutfitRecommendationDto { Type = type, TopCandidates = top }, impressions);
    }

    // ----- Shared helpers -------------------------------------------------------------------------

    protected static List<OutfitFeedback> BuildImpressions(
        Guid userId, Guid generationId, ClothingType type, IReadOnlyList<SimilarItemDto> shown, OutfitGenerationContext context)
    {
        var impressions = new List<OutfitFeedback>(shown.Count);
        for (var rank = 0; rank < shown.Count; rank++)
        {
            var c = shown[rank];
            impressions.Add(new OutfitFeedback
            {
                UserId = userId,
                GenerationId = generationId,
                ClothingItemId = c.Id,
                SlotType = type,
                Rank = rank,
                Occasion = context.OccasionBucket
            });
        }
        return impressions;
    }

    // best-effort: feedback logging must never break generation.
    protected async Task LogImpressionsAsync(IReadOnlyList<OutfitFeedback> impressions, CancellationToken ct)
    {
        if (impressions.Count == 0) return;
        try { await feedbackRepository.AddImpressionsAsync(impressions, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to log {Count} outfit impressions.", impressions.Count);
        }
    }

    // human-readable note explaining what a slot had to relax (precise provenance from Stage 1).
    protected static string? DescribeRelaxation(ClothingType type, IReadOnlySet<ConstraintKind> relaxed, OutfitGenerationContext context)
    {
        if (relaxed.Count == 0) return null;
        var label = SlotLabel(type);
        context.GarmentConstraints.TryGetValue(type, out var spec);

        // report the single most-informative relaxation without wording it as a failed search.
        if (relaxed.Contains(ConstraintKind.Weather))
            return $"Weather suitability was relaxed for the {label}; selected the strongest available piece.";
        if (relaxed.Contains(ConstraintKind.Gender))
            return $"Fit targeting was relaxed for the {label}; selected the strongest available option.";
        if (relaxed.Contains(ConstraintKind.AvoidColor))
            return $"Color avoidance was relaxed for the {label}; wardrobe options were limited.";
        if (relaxed.Contains(ConstraintKind.Style) && !string.IsNullOrWhiteSpace(context.TargetStyle))
            return $"Style targeting was relaxed for the {label}; selected the strongest available {context.TargetStyle} option.";
        if (relaxed.Contains(ConstraintKind.DesiredColor) && spec is { DesiredColors.Count: > 0 })
            return $"Palette targeting was relaxed for the {label}; requested colors were limited.";
        if (relaxed.Contains(ConstraintKind.SubType) && !string.IsNullOrWhiteSpace(spec?.SubType))
            return $"Garment-type targeting was relaxed for the {label}; selected another {label}.";
        return null;
    }

    protected static string MissingSlotWarning(ClothingType type) =>
        $"Add a {SlotLabel(type)} to complete this outfit.";

    // per-slot notes when the final selection relaxed a requested constraint (covers the seed slot too;
    protected static IReadOnlyList<string> BuildConstraintWarnings(OutfitGenerationContext context)
    {
        var warnings = new List<string>();
        foreach (var item in context.SelectedItems)
        {
            if (!context.GarmentConstraints.TryGetValue(item.Type, out var spec)) continue;
            var label = SlotLabel(item.Type);

            if (!string.IsNullOrWhiteSpace(spec.SubType) &&
                !string.Equals(item.SubType, spec.SubType, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"Garment-type targeting was relaxed for the {label}; selected another {label}.");
            }

            if (spec.AvoidColors.Count > 0 && spec.AvoidColors.Any(a => ColorFamily.ColorsMatch(item.Color, a)))
            {
                warnings.Add($"Color avoidance was relaxed for the {label}; wardrobe options were limited.");
            }
            else if (spec.DesiredColors.Count > 0 && !spec.DesiredColors.Any(d => ColorFamily.ColorsMatch(item.Color, d)))
            {
                warnings.Add($"Palette targeting was relaxed for the {label}; requested colors were limited.");
            }
        }
        return warnings.Distinct().ToList();
    }

    protected static string Join(IReadOnlyList<string> values) => string.Join(" or ", values);

    protected static string SlotLabel(ClothingType type) => type switch
    {
        ClothingType.Top => "top",
        ClothingType.Bottom => "bottoms",
        ClothingType.Shoes => "shoes",
        ClothingType.Outerwear => "outerwear",
        ClothingType.Accessory => "accessory",
        _ => "item"
    };

    // element-wise mean of the selected items' embeddings, L2-normalized.
    protected static float[]? CentroidEmbedding(IReadOnlyCollection<ClothingItem> items)
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

    protected static List<ClothingType> GetNeededTypes(ClothingType startType, OutfitGenerationContext context)
    {
        var needed = new List<ClothingType>();
        void Add(ClothingType t) { if (startType != t && !needed.Contains(t)) needed.Add(t); }

        // default basic outfit.
        Add(ClothingType.Top);
        Add(ClothingType.Bottom);
        Add(ClothingType.Shoes);

        // outerwear: single shared policy (user mode + threshold, live weather, then the temperature hint).
        var wantOuterwear = OuterwearPolicy.ShouldIncludeOuterwear(
            context.OuterwearMode, context.OuterwearTempThreshold,
            context.Weather?.Temperature, context.TemperatureHint);
        if (wantOuterwear) Add(ClothingType.Outerwear);

        // honor explicitly requested types, then always offer an accessory.
        foreach (var t in context.RequestedTypes) Add(t);
        Add(ClothingType.Accessory);

        return needed;
    }

    // ----- Context assembly -----------------------------------------------------------------------

    private async Task<OutfitGenerationContext> BuildContextAsync(
        Guid userId, ClothingItem startItem, OutfitGenerationOptions options, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);

        IReadOnlyList<string> preferredColors =
            options.DesiredColors.Count == 0 && user?.FavoriteColors is { Count: > 0 } favs
                ? favs
                : new List<string>();

        var wearRecency = await clothingRepository.GetWearRecencyAsync(userId, ct);
        var wearCounts = await clothingRepository.GetWearCountsAsync(userId, ct) ?? new Dictionary<Guid, int>();
        var excludedItemIds = (await feedbackRepository.GetRejectedItemIdsSinceAsync(userId, DateTime.UtcNow.Date, ct))?.ToHashSet()
                              ?? new HashSet<Guid>();
        excludedItemIds.UnionWith(options.ExcludedItemIds);

        // items surfaced in the last few days feed cross-generation rotation.
        var recentlyShown = (await feedbackRepository.GetRecentlyShownItemIdsAsync(userId, DateTime.UtcNow.AddDays(-5), null, ct))?.ToHashSet()
                            ?? new HashSet<Guid>();

        var pairCompatibility = await pairScoreRepository.GetCompatibilityMapAsync(userId, ct) ?? EmptyPairMap;
        var globalProfile = await learningProfileRepository.GetByUserIdAsync(userId, ct);

        var occasionBucket = OccasionBucketFor(options, startItem);

        return new OutfitGenerationContext
        {
            Weather = options.Weather,
            TargetStyle = DeriveTargetStyle(options.Style, startItem),
            DesiredColors = options.DesiredColors,
            AvoidColors = options.AvoidColors,
            PreferredColors = preferredColors,
            SoftAvoidColors = user?.AvoidColors ?? new List<string>(),
            Occasion = options.Occasion,
            OccasionBucket = occasionBucket,
            Formality = options.Formality,
            TargetGender = NormalizeGender(startItem.Gender),
            TemperatureHint = options.TemperatureHint,
            OuterwearMode = user?.OuterwearMode,
            OuterwearTempThreshold = user?.OuterwearTempThreshold ?? 23,
            PreferLightOnHotDays = user?.PreferLightOnHotDays ?? true,
            RequestedTypes = options.RequestedTypes,
            GarmentConstraints = options.GarmentConstraints,
            WearRecency = wearRecency,
            WearCounts = wearCounts,
            MedianWearCount = Median(wearCounts.Values),
            VarietyDaysFactor = VarietyDaysFactorFor(user?.VarietyLevel),
            ExcludedItemIds = excludedItemIds,
            RecentlyRecommendedItemIds = recentlyShown,
            PreferUnusedItems = options.PreferUnusedItems,
            SelectedItems = { startItem },
            PairCompatibility = pairCompatibility,
            LearnedColorScores = globalProfile?.ColorScores ?? EmptyScores,
            LearnedStyleScores = globalProfile?.StyleScores ?? EmptyScores
        };
    }

    private static string OccasionBucketFor(OutfitGenerationOptions options, ClothingItem startItem)
    {
        var raw = options.Occasion ?? options.Style ?? startItem.Usage?.Split(',')[0];
        var trimmed = raw?.Trim().ToLowerInvariant();
        return string.IsNullOrEmpty(trimmed) ? "general" : trimmed;
    }

    private static double VarietyDaysFactorFor(string? varietyLevel) => varietyLevel?.ToLowerInvariant() switch
    {
        "high" => 0.66,
        "low" => 1.5,
        _ => 1.0
    };

    private static readonly IReadOnlyDictionary<string, double> EmptyScores = new Dictionary<string, double>();
    private static readonly IReadOnlyDictionary<(Guid, Guid), double> EmptyPairMap = new Dictionary<(Guid, Guid), double>();

    private static double Median(ICollection<int> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToArray();
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
    }

    private static string? NormalizeGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender)) return null;
        return gender.Equals("Unisex", StringComparison.OrdinalIgnoreCase) ? null : gender;
    }

    private static string? DeriveTargetStyle(string? explicitStyle, ClothingItem startItem)
    {
        if (!string.IsNullOrWhiteSpace(explicitStyle)) return explicitStyle;
        if (string.IsNullOrWhiteSpace(startItem.Usage)) return null;
        return startItem.Usage.Split(',')[0].Trim();
    }

    // ----- result records -------------------------------------------------------------------------

    protected sealed record SlotPlan(
        List<SimilarItemDto> Selected,
        List<OutfitRecommendationDto> Recommendations,
        List<OutfitFeedback> Impressions,
        List<string> Warnings,
        bool IsValid,
        IReadOnlyList<OutfitCandidate> Candidates);

    protected sealed record SlotPool(
        ClothingType Type,
        IReadOnlyList<FeasibleCandidate> Candidates,
        IReadOnlySet<ConstraintKind> Relaxed,
        bool IsEmptyWardrobeSlot);
}
