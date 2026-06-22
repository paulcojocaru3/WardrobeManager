using Microsoft.Extensions.Logging;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Feasibility;
using WardrobeManager.Application.Outfits.Scoring;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Application.Outfits.Generation;

// production default. Instead of committing to the single best piece per slot (greedy), it keeps the
public sealed class BeamSearchOutfitGenerator(
    IClothingRepository clothingRepository,
    IUserRepository userRepository,
    IOutfitFeedbackRepository feedbackRepository,
    IItemPairScoreRepository pairScoreRepository,
    IUserLearningProfileRepository learningProfileRepository,
    IEnumerable<IOutfitEvaluator> evaluators,
    IGarmentFeasibility feasibility,
    ILogger<BeamSearchOutfitGenerator> logger)
    : OutfitGeneratorBase(clothingRepository, userRepository, feedbackRepository, pairScoreRepository,
        learningProfileRepository, evaluators, feasibility, logger)
{
    private const int BeamWidth = 5;

    protected override async Task<SlotPlan> PlanSlotsAsync(
        Guid userId, ClothingItem startItem, OutfitGenerationContext context, Guid generationId,
        IReadOnlyList<ClothingType> neededTypes, OutfitGenerationOptions options, CancellationToken ct)
    {
        // feasibility is context-level (independent of the partial outfit), so each slot's feasible pool
        var slotPools = new Dictionary<ClothingType, SlotPool>();

        var beam = new List<BeamState> { new(new List<ClothingItem> { startItem }, 0.0, new List<SlotChoice>()) };

        foreach (var type in neededTypes)
        {
            // anchor retrieval on the best current partial outfit for cohesion.
            var anchor = CentroidEmbedding(beam[0].Items) ?? startItem.Embedding!;
            var raw = (await RetrieveAsync(userId, anchor, type, context, ct))
                .Where(r => r.Item.Id != startItem.Id)
                .ToList();
            var pool = BuildSlotPool(type, raw, context);
            slotPools[type] = pool;

            if (pool.Candidates.Count == 0) continue; // truly no item of this type; handled at assembly

            var children = new List<BeamState>(beam.Count * pool.Candidates.Count);
            foreach (var state in beam)
            {
                context.SelectedItems = new List<ClothingItem>(state.Items); // score against this state
                foreach (var cand in pool.Candidates)
                {
                    var score = ScoreItemSoft(cand.Item, cand.Similarity, context);
                    var items = new List<ClothingItem>(state.Items) { cand.Item };
                    var choices = new List<SlotChoice>(state.Choices) { new(type, cand.Item, cand.Relaxed) };
                    children.Add(new BeamState(items, state.CumScore + score, choices));
                }
            }

            beam = children.OrderByDescending(s => s.CumScore).Take(BeamWidth).ToList();
        }

        var winner = beam.OrderByDescending(s => s.CumScore).First();
        var chosenByType = winner.Choices.ToDictionary(c => c.Type);

        // anchor formality on the WHOLE chosen outfit so swap alternatives are judged against the look as a
        context.OutfitFormalityRank = FormalityScale.MedianKnownRank(winner.Items);

        var selected = new List<SimilarItemDto>
        {
            new() { Id = startItem.Id, Name = startItem.Name, ProcessedImageUrl = startItem.ProcessedImageUrl, SimilarityScore = 1.0 }
        };
        var recommendations = new List<OutfitRecommendationDto>();
        var impressions = new List<OutfitFeedback>();
        var warnings = new List<string>();
        var isValid = true;

        foreach (var type in neededTypes)
        {
            var pool = slotPools[type];
            if (pool.Candidates.Count == 0)
            {
                recommendations.Add(new OutfitRecommendationDto { Type = type, TopCandidates = [] });
                if (EssentialTypes.Contains(type))
                {
                    warnings.Add(MissingSlotWarning(type));
                    isValid = false;
                }
                continue;
            }

            var choice = chosenByType[type];

            // rank this slot's alternatives against the FINAL outfit minus the chosen piece, so the
            context.SelectedItems = winner.Items.Where(i => i.Id != choice.Item.Id).ToList();
            var rawScored = pool.Candidates
                .Select(c => new ScoredCandidate(c.Item, ScoreItemSoft(c.Item, c.Similarity, context), c.Relaxed))
                .ToList();

            // honest scoring: show each alternative RELATIVE TO THE CHOSEN PIECE (the pick = 100%), not to
            var chosenRaw = rawScored.First(c => c.Item.Id == choice.Item.Id).Score;
            double scaleFactor = Math.Max(chosenRaw, 1e-6);

            var scored = rawScored
                .Select(c => new ScoredCandidate(c.Item, Math.Min(1.0, c.Score / scaleFactor), c.Relaxed))
                .OrderByDescending(s => s.Score)
                .ToList();

            var chosenScored = scored.First(s => s.Item.Id == choice.Item.Id);
            var alternatives = scored.Where(s => s.Item.Id != choice.Item.Id).Take(ShownPerSlot - 1);
            var top = new[] { chosenScored }.Concat(alternatives).Select(ToDto).ToList();

            recommendations.Add(new OutfitRecommendationDto { Type = type, TopCandidates = top });
            impressions.AddRange(BuildImpressions(userId, generationId, type, top, context));

            selected.Add(ToDto(chosenScored));
            var note = DescribeRelaxation(type, choice.Relaxed, context);
            if (note != null) warnings.Add(note);
            if (chosenScored.Score < options.Threshold) isValid = false;
        }

        var outfitCandidates = new List<OutfitCandidate>();
        int candidateId = 0;
        foreach (var state in beam.OrderByDescending(s => s.CumScore))
        {
            var candidateItems = state.Items.Select(i => new CandidateItem(i.Id, i.Type.ToString(), i.ProcessedImageUrl)).ToList();
            outfitCandidates.Add(new OutfitCandidate(candidateId++, state.CumScore, candidateItems));
        }

        context.SelectedItems = new List<ClothingItem>(winner.Items); // final outfit for base warnings
        return new SlotPlan(selected, recommendations, impressions, warnings, isValid, outfitCandidates);
    }

    private static SimilarItemDto ToDto(ScoredCandidate c) => new()
    {
        Id = c.Item.Id,
        Name = c.Item.Name,
        ProcessedImageUrl = c.Item.ProcessedImageUrl,
        SimilarityScore = Math.Max(0, c.Score)
    };

    private sealed record BeamState(List<ClothingItem> Items, double CumScore, List<SlotChoice> Choices);

    private sealed record SlotChoice(ClothingType Type, ClothingItem Item, IReadOnlySet<ConstraintKind> Relaxed);

    private sealed record ScoredCandidate(ClothingItem Item, double Score, IReadOnlySet<ConstraintKind> Relaxed);
}
