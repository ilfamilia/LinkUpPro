using LinkUpPro.Core.Domain.Entities;

namespace LinkUpPro.Core.Domain.Interfaces;

/// <summary>Loads a complete game aggregate while reusing common repository operations.</summary>
public interface IBattleshipRepository : IGenericRepository<BattleshipGame>
{
    Task<BattleshipGame?> GetGameAsync(int id);
}

