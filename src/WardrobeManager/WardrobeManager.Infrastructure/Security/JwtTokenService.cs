using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Infrastructure.Security;

// HS256 JWTs; key/issuer/audience/lifetime come from the "Jwt" config section
public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string GenerateToken(User user)
    {
        var key = configuration["Jwt:Key"];
        if (key == null)
        {
            throw new InvalidOperationException("Jwt:Key is not configured.");
        }

        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        int expiryMinutes;
        if (int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m))
        {
            expiryMinutes = m;
        }
        else
        {
            expiryMinutes = 1440;
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
