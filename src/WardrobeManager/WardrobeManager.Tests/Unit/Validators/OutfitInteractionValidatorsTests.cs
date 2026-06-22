using WardrobeManager.Application.Outfits.Commands;
using WardrobeManager.Application.Outfits.Queries;
using WardrobeManager.Application.Outfits.Validators;

namespace WardrobeManager.Tests.Unit.Validators;

[Trait("Category", "Unit")]
public sealed class OutfitInteractionValidatorsTests
{
    [Fact]
    public void ToggleFavorite_Fails_WhenIdsEmpty()
    {
        var result = new ToggleOutfitFavoriteCommandValidator()
            .Validate(new ToggleOutfitFavoriteCommand(Guid.Empty, Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RecordWear_Passes_WhenIdsPresent()
    {
        var result = new RecordOutfitWearCommandValidator()
            .Validate(new RecordOutfitWearCommand(Guid.NewGuid(), Guid.NewGuid()));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void RecordFeedback_Fails_WhenItemsEmpty()
    {
        var cmd = new RecordOutfitFeedbackCommand(Guid.NewGuid(), Guid.NewGuid(), new List<OutfitFeedbackItem>());
        Assert.False(new RecordOutfitFeedbackCommandValidator().Validate(cmd).IsValid);
    }

    [Fact]
    public void RecordFeedback_Fails_WhenActionUnsupported()
    {
        var cmd = new RecordOutfitFeedbackCommand(Guid.NewGuid(), Guid.NewGuid(),
            new List<OutfitFeedbackItem> { new(Guid.NewGuid(), "teleport") });
        Assert.False(new RecordOutfitFeedbackCommandValidator().Validate(cmd).IsValid);
    }

    [Fact]
    public void RecordFeedback_Passes_WhenActionKnown()
    {
        var cmd = new RecordOutfitFeedbackCommand(Guid.NewGuid(), Guid.NewGuid(),
            new List<OutfitFeedbackItem> { new(Guid.NewGuid(), "accepted") });
        Assert.True(new RecordOutfitFeedbackCommandValidator().Validate(cmd).IsValid);
    }

    [Fact]
    public void GetLearnedProfile_Fails_WhenUserEmpty()
    {
        Assert.False(new GetLearnedProfileQueryValidator().Validate(new GetLearnedProfileQuery(Guid.Empty)).IsValid);
    }

    [Fact]
    public void ExplainOutfit_Fails_WhenItemsEmpty()
    {
        var query = new ExplainOutfitQuery(Guid.NewGuid(), Array.Empty<Guid>(), null, null, null);
        Assert.False(new ExplainOutfitQueryValidator().Validate(query).IsValid);
    }

    [Fact]
    public void OutfitInsight_Fails_WhenStyleTooLong()
    {
        var query = new OutfitInsightQuery(Guid.NewGuid(), new[] { Guid.NewGuid() }, new string('x', 81), null, null);
        Assert.False(new OutfitInsightQueryValidator().Validate(query).IsValid);
    }

    [Fact]
    public void OutfitInsight_Passes_WhenWithinLimits()
    {
        var query = new OutfitInsightQuery(Guid.NewGuid(), new[] { Guid.NewGuid() }, "casual", "work", "Rome");
        Assert.True(new OutfitInsightQueryValidator().Validate(query).IsValid);
    }
}
