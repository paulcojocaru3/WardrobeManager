using NSubstitute;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Application.Users.Commands;
using WardrobeManager.Application.Users.Queries;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public sealed class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();

    private RegisterUserCommandHandler Sut()
        => new(_users, _hasher, _jwt);

    private static RegisterUserCommand Command() => new("alice@example.com", "passw0rd", "alice");

    [Fact]
    public async Task Handle_CreatesUser_AndReturnsToken()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _hasher.Hash("passw0rd").Returns("hashed");
        _jwt.GenerateToken(Arg.Any<User>()).Returns("jwt-token");

        var result = await Sut().Handle(Command(), CancellationToken.None);

        Assert.Equal("jwt-token", result.Token);
        Assert.Equal("alice@example.com", result.User.Email);
        await _users.Received(1).AddAsync(Arg.Is<User>(u => u.PasswordHash == "hashed"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenEmailInUse()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new User());

        await Assert.ThrowsAsync<Exception>(() => Sut().Handle(Command(), CancellationToken.None));
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenUsernameTaken()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new User());

        await Assert.ThrowsAsync<Exception>(() => Sut().Handle(Command(), CancellationToken.None));
    }

}

[Trait("Category", "Unit")]
public sealed class LoginUserQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();

    private LoginUserQueryHandler Sut() => new(_users, _hasher, _jwt);
    private static LoginUserQuery Query() => new("alice@example.com", "secret");

    [Fact]
    public async Task Handle_ReturnsToken_OnValidCredentials()
    {
        var user = new User { Email = "alice@example.com", PasswordHash = "hash" };
        _users.GetByEmailAsync("alice@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("secret", "hash").Returns(true);
        _jwt.GenerateToken(user).Returns("jwt-token");

        var result = await Sut().Handle(Query(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("jwt-token", result!.Token);
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenUserNotFound()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        Assert.Null(await Sut().Handle(Query(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ReturnsNull_WhenPasswordWrong()
    {
        var user = new User { Email = "alice@example.com", PasswordHash = "hash" };
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        Assert.Null(await Sut().Handle(Query(), CancellationToken.None));
    }
}

[Trait("Category", "Unit")]
public sealed class UpdateUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();

    private UpdateUserCommandHandler Sut() => new(_users, _hasher);

    private (UpdateUserCommand cmd, User user) Setup(string? username = null, string? email = null, string? newPassword = null)
    {
        var user = new User { Id = Guid.NewGuid(), Username = "alice", Email = "alice@example.com", PasswordHash = "hash" };
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var cmd = new UpdateUserCommand(user.Id, username, email, newPassword, "current");
        return (cmd, user);
    }

    [Fact]
    public async Task Handle_UpdatesUsername()
    {
        var (cmd, user) = Setup(username: "alice2");
        _users.GetByUsernameAsync("alice2", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await Sut().Handle(cmd, CancellationToken.None);

        Assert.Equal("alice2", result.Username);
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenUserNotFound()
    {
        var cmd = new UpdateUserCommand(Guid.NewGuid(), "alice2", null, null, "current");
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<Exception>(() => Sut().Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_WhenCurrentPasswordWrong()
    {
        var (cmd, _) = Setup(username: "alice2");
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await Assert.ThrowsAsync<Exception>(() => Sut().Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_WhenUsernameTaken()
    {
        var (cmd, _) = Setup(username: "taken");
        _users.GetByUsernameAsync("taken", Arg.Any<CancellationToken>()).Returns(new User());

        await Assert.ThrowsAsync<Exception>(() => Sut().Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HashesNewPassword()
    {
        var (cmd, user) = Setup(newPassword: "newpass1");
        _hasher.Hash("newpass1").Returns("new-hash");

        await Sut().Handle(cmd, CancellationToken.None);

        Assert.Equal("new-hash", user.PasswordHash);
    }
}

[Trait("Category", "Unit")]
public sealed class DeleteUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private DeleteUserCommandHandler Sut() => new(_users);

    [Fact]
    public async Task Handle_DeletesUser()
    {
        var user = new User { Id = Guid.NewGuid() };
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await Sut().Handle(new DeleteUserCommand(user.Id), CancellationToken.None);

        await _users.Received(1).DeleteAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Throws_WhenUserNotFound()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        await Assert.ThrowsAsync<Exception>(() => Sut().Handle(new DeleteUserCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
