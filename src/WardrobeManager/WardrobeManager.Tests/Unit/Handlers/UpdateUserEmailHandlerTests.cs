using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Users.Commands;
using WardrobeManager.Application.Users.Validators;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class UpdateUserEmailHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly Guid _userId = Guid.NewGuid();

    private UpdateUserCommandHandler Sut() => new(_users, _hasher, new UpdateUserCommandValidator());

    private User OwnedUser()
    {
        var user = new User { Id = _userId, Username = "alice", Email = "alice@old.com", PasswordHash = "hash" };
        _users.GetByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    [Fact]
    public async Task ChangesEmail_WhenNewAndAvailable()
    {
        var user = OwnedUser();
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _users.GetByEmailAsync("alice@new.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var cmd = new UpdateUserCommand(_userId, null, "alice@new.com", null, "current");
        var dto = await Sut().Handle(cmd, CancellationToken.None);

        Assert.Equal("alice@new.com", user.Email);
        Assert.Equal("alice@new.com", dto.Email);
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_WhenEmailAlreadyInUse()
    {
        var user = OwnedUser();
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _users.GetByEmailAsync("taken@new.com", Arg.Any<CancellationToken>())
              .Returns(new User { Id = Guid.NewGuid(), Email = "taken@new.com" });

        var cmd = new UpdateUserCommand(_userId, null, "taken@new.com", null, "current");

        await Assert.ThrowsAsync<Exception>(() => Sut().Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_WhenChangingEmailWithWrongCurrentPassword()
    {
        OwnedUser();
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var cmd = new UpdateUserCommand(_userId, null, "alice@new.com", null, "wrong");

        await Assert.ThrowsAsync<Exception>(() => Sut().Handle(cmd, CancellationToken.None));
    }
}
