using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Infrastructure.Security;

namespace WardrobeManager.Tests.Unit.Security;

[Trait("Category", "Unit")]
public sealed class JwtTokenServiceTests
{
    // generated per run (not a hardcoded secret) so the signing key never appears as a literal.
    private static readonly string Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    private static IConfiguration Config(params (string Key, string? Value)[] overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = Key,
            ["Jwt:Issuer"] = "WardrobeManager",
            ["Jwt:Audience"] = "WardrobeManager",
            ["Jwt:ExpiryMinutes"] = "60",
        };
        foreach (var (k, v) in overrides) values[k] = v;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static User User() => new()
    {
        Id = Guid.NewGuid(),
        Username = "alice",
        Email = "alice@example.com",
    };

    [Fact]
    public void GenerateToken_EmbedsUserClaims()
    {
        var user = User();
        var token = new JwtTokenService(Config()).GenerateToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("WardrobeManager", jwt.Issuer);
        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("alice@example.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    }

    [Fact]
    public void GenerateToken_Throws_WhenKeyMissing()
    {
        var config = Config(("Jwt:Key", null));
        Assert.Throws<InvalidOperationException>(() => new JwtTokenService(config).GenerateToken(User()));
    }

    [Fact]
    public void GenerateToken_FallsBackToDefaultExpiry_WhenInvalid()
    {
        var config = Config(("Jwt:ExpiryMinutes", "not-a-number"));
        var token = new JwtTokenService(config).GenerateToken(User());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        // default 1440 minutes (24h) from now
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddHours(23));
    }
}
