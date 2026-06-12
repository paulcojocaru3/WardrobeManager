using WardrobeManager.Domain.Entities;

namespace WardrobeManager.Application.Abstractions;

public interface IUserEvaluatorWeightsRepository
{
    Task<UserEvaluatorWeights?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(UserEvaluatorWeights weights, CancellationToken ct = default);
}
