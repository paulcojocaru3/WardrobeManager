using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Outfits.Learning;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Domain.Enums;

namespace WardrobeManager.Tests.Unit.Learning;

[Trait("Category", "Unit")]
public sealed class UserLearningProfileServiceTests
{
    private readonly IUserLearningProfileRepository _repo = Substitute.For<IUserLearningProfileRepository>();
    private UserLearningProfileService Sut() => new(_repo, NullLogger<UserLearningProfileService>.Instance);

    private async Task<UserLearningProfile?> Capture(params ActionedItem[] items)
    {
        return await CaptureForOccasion(null, items);
    }

    private async Task<UserLearningProfile?> CaptureForOccasion(string? occasion, params ActionedItem[] items)
    {
        UserLearningProfile? saved = null;
        _repo.UpsertAsync(Arg.Do<UserLearningProfile>(p => saved = p), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        await Sut().UpdateAsync(Guid.NewGuid(), occasion, items);
        return saved;
    }

    [Fact]
    public async Task Update_PositiveItem_MovesColorAndStyleUp()
    {
        var item = new ActionedItem(TestData.Item(color: "navy", usage: "Casual"), FeedbackAction.Accepted, 0);

        var saved = await Capture(item);

        Assert.NotNull(saved);
        // navy normalizes to "blue"; from neutral 0.5, one positive nudge -> 0.15*1 + 0.85*0.5 = 0.575
        Assert.Equal(0.575, saved!.ColorScores["blue"], 3);
        Assert.Equal(0.575, saved.StyleScores["casual"], 3);
    }

    [Fact]
    public async Task Update_ActiveSwapOut_MovesDown()
    {
        var item = new ActionedItem(TestData.Item(color: "blue"), FeedbackAction.Rejected, 0);

        var saved = await Capture(item);

        // from neutral 0.5, one negative nudge -> 0.15*0 + 0.85*0.5 = 0.425
        Assert.Equal(0.425, saved!.ColorScores["blue"], 3);
    }

    [Fact]
    public async Task Update_BlendsOntoExistingScore()
    {
        _repo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new UserLearningProfile { ColorScores = new() { ["blue"] = 0.8 } });
        var item = new ActionedItem(TestData.Item(color: "blue"), FeedbackAction.Worn, 0);

        var saved = await Capture(item);

        // 0.15*1 + 0.85*0.8 = 0.83
        Assert.Equal(0.83, saved!.ColorScores["blue"], 3);
    }

    [Fact]
    public async Task Update_IgnoresLowerRankedRejections()
    {
        await Sut().UpdateAsync(Guid.NewGuid(), null, new[] { new ActionedItem(TestData.Item(color: "blue"), FeedbackAction.Rejected, 3) });

        await _repo.DidNotReceive().UpsertAsync(Arg.Any<UserLearningProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_UsesSingleGlobalProfile_WhenOccasionGiven()
    {
        var item = new ActionedItem(TestData.Item(color: "navy", usage: "Casual"), FeedbackAction.Worn, 0);

        var saved = await CaptureForOccasion("work", item);

        Assert.NotNull(saved);
        Assert.True(saved!.ColorScores.ContainsKey("blue"));
    }
}
