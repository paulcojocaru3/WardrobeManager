using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public sealed class UserEvaluatorWeightsRepository : IUserEvaluatorWeightsRepository
{
    private readonly ApplicationDbContext _context;

    public UserEvaluatorWeightsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserEvaluatorWeights?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.UserEvaluatorWeights
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);
    }

    public async Task UpsertAsync(UserEvaluatorWeights weights, CancellationToken ct = default)
    {
        var existing = await _context.UserEvaluatorWeights.FirstOrDefaultAsync(w => w.UserId == weights.UserId, ct);

        if (existing == null)
        {
            await _context.UserEvaluatorWeights.AddAsync(weights, ct);
        }
        else
        {
            existing.Weights = weights.Weights;
            existing.MlWeight = weights.MlWeight;
            existing.TrainedOnSamples = weights.TrainedOnSamples;
            existing.UpdatedAt = weights.UpdatedAt;
        }

        await _context.SaveChangesAsync(ct);
    }
}
