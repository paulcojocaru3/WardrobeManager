using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
}