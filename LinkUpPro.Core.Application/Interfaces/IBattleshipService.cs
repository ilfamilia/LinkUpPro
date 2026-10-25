using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.Core.Application.Dtos.Social;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Interfaces;

/// <summary>Coordinates persisted games, trusted participants, stale-form protection, and private projections.</summary>
public interface IBattleshipService
{
    /// <summary>Lists the caller's games and evaluates abandoned turns.</summary>
    Task<List<GameSummaryDto>> GetGamesAsync();
    /// <summary>Finds active friends without an unfinished game against the caller.</summary>
    Task<List<UserSummaryDto>> GetEligibleOpponentsAsync(string? search = null);
    /// <summary>Creates one unfinished game for a currently eligible friend pair.</summary>
    Task<int> CreateAsync(string opponentId);
    /// <summary>Returns only authorized board data and evaluates the current turn deadline.</summary>
    Task<GameDetailsDto> GetGameAsync(int gameId);
    /// <summary>Places a ship after checking identity, phase, version, and geometry.</summary>
    Task PlaceShipAsync(int gameId, long expectedVersion, int shipNumber, int row, int column, ShipDirection direction);
    /// <summary>Records one attack together with its result and next turn.</summary>
    Task AttackAsync(int gameId, long expectedVersion, int row, int column);
    /// <summary>Awards the opponent victory after checking identity and expected version.</summary>
    Task SurrenderAsync(int gameId, long expectedVersion);
}
