using WardrobeManager.Infrastructure.Security;

namespace WardrobeManager.Tests.Unit.Security;

[Trait("Category", "Unit")]
public sealed class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ProducesValueDifferentFromInput()
    {
        var hash = _sut.Hash("passw0rd");
        Assert.False(string.IsNullOrEmpty(hash));
        Assert.NotEqual("passw0rd", hash);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hash = _sut.Hash("passw0rd");
        Assert.True(_sut.Verify("passw0rd", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = _sut.Hash("passw0rd");
        Assert.False(_sut.Verify("nope", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Verify_ReturnsFalse_ForBlankHash(string hash)
    {
        Assert.False(_sut.Verify("passw0rd", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForNonBCryptHash()
    {
        // legacy plaintext / malformed value -> SaltParseException is swallowed
        Assert.False(_sut.Verify("passw0rd", "not-a-bcrypt-hash"));
    }
}
