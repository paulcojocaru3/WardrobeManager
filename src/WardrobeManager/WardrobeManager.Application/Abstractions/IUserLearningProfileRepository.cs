using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface IUserLearningProfileRepository
{
    Task<UserLearningProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(UserLearningProfile profile, CancellationToken ct = default);
}
