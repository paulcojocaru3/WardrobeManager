using Microsoft.EntityFrameworkCore;
using WardrobeManager.Application.Abstractions;
using WardrobeManager.Domain.Entities;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Infrastructure.Repositories;

public sealed class UserLearningProfileRepository : IUserLearningProfileRepository
{
    private readonly ApplicationDbContext _context;

    public UserLearningProfileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserLearningProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.UserLearningProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    public async Task UpsertAsync(UserLearningProfile profile, CancellationToken ct = default)
    {
        var existing = await _context.UserLearningProfiles.FirstOrDefaultAsync(p => p.UserId == profile.UserId, ct);

        if (existing == null)
        {
            await _context.UserLearningProfiles.AddAsync(profile, ct);
        }
        else
        {
            existing.ColorScores = profile.ColorScores;
            existing.StyleScores = profile.StyleScores;
            existing.UpdatedAt = profile.UpdatedAt;
        }

        await _context.SaveChangesAsync(ct);
    }
}
