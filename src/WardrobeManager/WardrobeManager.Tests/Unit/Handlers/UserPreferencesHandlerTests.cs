using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Users.Commands;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class UserPreferencesHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private UpdateUserPreferencesCommandHandler Sut() => new(_users);

    private User OwnedUser()
    {
        var user = new User { Id = _userId, Username = "alice", Email = "a@b.c", PasswordHash = "h" };
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    [Fact]
    public async Task Throws_WhenUserNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var cmd = new UpdateUserPreferencesCommand(_userId, null, null, null);

        await Assert.ThrowsAsync<Exception>(() => Sut().Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task NormalizesFavoriteColors_TrimLowercaseDistinct()
    {
        var user = OwnedUser();

        var cmd = new UpdateUserPreferencesCommand(_userId, new List<string> { " Red ", "RED", "blue", "   " }, null, null);
        await Sut().Handle(cmd, CancellationToken.None);

        Assert.Equal(new[] { "red", "blue" }, user.FavoriteColors);
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetsPreferredCity_AndClearsOnBlank()
    {
        var user = OwnedUser();

        await Sut().Handle(new UpdateUserPreferencesCommand(_userId, null, " Rome ", null), CancellationToken.None);
        Assert.Equal("Rome", user.PreferredCity);

        await Sut().Handle(new UpdateUserPreferencesCommand(_userId, null, "   ", null), CancellationToken.None);
        Assert.Null(user.PreferredCity);
    }

    [Fact]
    public async Task SetsTheme_AndClearsOnBlank()
    {
        var user = OwnedUser();

        await Sut().Handle(new UpdateUserPreferencesCommand(_userId, null, null, " dark "), CancellationToken.None);
        Assert.Equal("dark", user.ThemePreference);

        await Sut().Handle(new UpdateUserPreferencesCommand(_userId, null, null, ""), CancellationToken.None);
        Assert.Null(user.ThemePreference);
    }

    [Theory]
    [InlineData("always", "always")]
    [InlineData("NEVER", "never")]
    [InlineData("nonsense", "auto")] // unrecognized -> auto
    public async Task NormalizesOuterwearMode(string input, string expected)
    {
        var user = OwnedUser();

        await Sut().Handle(new UpdateUserPreferencesCommand(_userId, null, null, null, OuterwearMode: input), CancellationToken.None);

        Assert.Equal(expected, user.OuterwearMode);
    }

    [Theory]
    [InlineData(2, 5)]    // below minimum -> clamped up
    [InlineData(40, 30)]  // above maximum -> clamped down
    [InlineData(18, 18)]  // within range -> unchanged
    public async Task ClampsOuterwearThreshold(int input, int expected)
    {
        var user = OwnedUser();

        await Sut().Handle(new UpdateUserPreferencesCommand(_userId, null, null, null, OuterwearTempThreshold: input), CancellationToken.None);

        Assert.Equal(expected, user.OuterwearTempThreshold);
    }

    [Fact]
    public async Task SavesAndDisablesDefaultReuseInterval()
    {
        var user = OwnedUser();

        await Sut().Handle(new UpdateUserPreferencesCommand(
            _userId, null, null, null,
            DefaultReuseAfterDays: 7,
            UpdateDefaultReuseAfterDays: true), CancellationToken.None);
        Assert.Equal(7, user.DefaultReuseAfterDays);

        await Sut().Handle(new UpdateUserPreferencesCommand(
            _userId, null, null, null,
            DefaultReuseAfterDays: null,
            UpdateDefaultReuseAfterDays: true), CancellationToken.None);
        Assert.Null(user.DefaultReuseAfterDays);
    }
}
