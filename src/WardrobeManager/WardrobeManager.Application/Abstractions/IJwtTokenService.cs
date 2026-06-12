using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
