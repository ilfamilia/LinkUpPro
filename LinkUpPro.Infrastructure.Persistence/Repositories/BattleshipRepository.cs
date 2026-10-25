using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Interfaces;
using LinkUpPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Infrastructure.Persistence.Repositories;

/// <summary>Loads complete game aggregates for invariant checks and atomic state transitions.</summary>
public sealed class BattleshipRepository(LinkUpProAppContext context)
    : GenericRepository<BattleshipGame>(context), IBattleshipRepository
{
    /// <summary>Includes both fleets and attack logs only inside the trusted persistence boundary.</summary>
    public Task<BattleshipGame?> GetGameAsync(int id) => _dbSet
        .Include(g => g.Ships).Include(g => g.Attacks).AsSplitQuery().SingleOrDefaultAsync(g => g.Id == id);
}
