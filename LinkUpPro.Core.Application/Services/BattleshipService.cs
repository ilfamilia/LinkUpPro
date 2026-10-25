using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.Core.Application.Dtos.Social;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.Core.Domain.Interfaces;

namespace LinkUpPro.Core.Application.Services;

/// <summary>Coordinates game persistence and projects only the boards each participant may inspect.</summary>
public sealed class BattleshipService(
    IBattleshipRepository games, IFriendshipService friends, IUserDirectoryService users,
    SocialUserGuard guard, IAtomicOperation atomic, TimeProvider clock, IMapper mapper) : IBattleshipService
{
    /// <summary>Evaluates expired turns while listing only games in which the caller participates.</summary>
    public async Task<List<GameSummaryDto>> GetGamesAsync()
    {
        var current = await guard.GetCurrentAsync();
        var list = await games.GetListAsync(g => g.User1Id == current.Id || g.User2Id == current.Id);
        var result = new List<GameSummaryDto>();
        foreach (var item in list.OrderByDescending(g => g.CreatedAt))
        {
            var game = await atomic.ExecuteAsync(async () =>
            {
                var loaded = await GetOwnedGameAsync(item.Id, current.Id);
                loaded.ExpireIfAbandoned(clock.GetUtcNow().UtcDateTime);
                return loaded;
            });
            result.Add(await ProjectSummaryAsync(game, current.Id));
        }
        return result;
    }

    /// <summary>Lists active friends without another unfinished game against the caller.</summary>
    public async Task<List<UserSummaryDto>> GetEligibleOpponentsAsync(string? search = null)
    {
        var current = await guard.GetCurrentAsync();
        await GetGamesAsync();
        var active = await games.GetListAsync(g => g.Status != BattleshipStatus.Finished
            && (g.User1Id == current.Id || g.User2Id == current.Id));
        var opponents = active.Select(g => g.GetOpponentId(current.Id)).ToHashSet();
        var term = search?.Trim() ?? "";
        return (await friends.GetFriendsAsync()).Select(f => f.User).Where(u => !opponents.Contains(u.Id)
            && (term.Length == 0 || u.UserName.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    /// <summary>Creates one game per active friend pair after rechecking current eligibility.</summary>
    public Task<int> CreateAsync(string opponentId) => atomic.ExecuteAsync(async () =>
    {
        var current = await guard.GetCurrentAsync();
        opponentId = (await guard.GetActiveAsync(opponentId)).Id;
        if (!await friends.AreFriendsAsync(current.Id, opponentId))
            throw new DomainRuleException("Solo puede iniciar partidas con amigos activos.");
        var pair = FriendshipService.OrderPair(current.Id, opponentId);
        var existing = await games.GetListAsync(g => g.User1Id == pair.First && g.User2Id == pair.Second
            && g.Status != BattleshipStatus.Finished);
        foreach (var previous in existing)
        {
            var loaded = await games.GetGameAsync(previous.Id);
            if (loaded!.ExpireIfAbandoned(clock.GetUtcNow().UtcDateTime))
                await games.UpdateAsync(loaded, loaded.Id); // Release the filtered active-pair key before inserting its replacement.
            if (loaded.Status != BattleshipStatus.Finished)
                throw new DomainRuleException("Ya existe una partida activa con este amigo.");
        }
        var game = await games.AddAsync(BattleshipGame.Create(current.Id, opponentId, clock.GetUtcNow().UtcDateTime));
        return game.Id;
    });

    /// <summary>Returns caller-owned setup or attack data and evaluates abandonment on refresh.</summary>
    public async Task<GameDetailsDto> GetGameAsync(int gameId)
    {
        var current = await guard.GetCurrentAsync();
        var game = await atomic.ExecuteAsync(async () =>
        {
            var loaded = await GetOwnedGameAsync(gameId, current.Id);
            loaded.ExpireIfAbandoned(clock.GetUtcNow().UtcDateTime);
            return loaded;
        });
        var summary = await ProjectSummaryAsync(game, current.Id);
        var dto = mapper.Map<GameDetailsDto>(game);
        dto.Opponent = summary.Opponent;
        dto.DurationHours = summary.DurationHours;
        dto.IsWinner = summary.IsWinner;
        dto.WinnerUserName = summary.WinnerUserName;
        dto.IsOwnTurn = game.Status == BattleshipStatus.Active && game.TurnUserId == current.Id;
        dto.TurnUserName = game.TurnUserId == current.Id ? "Yo" : summary.Opponent.UserName;
        dto.UnplacedShipNumbers = Enumerable.Range(0, 5)
            .Where(n => !game.Ships.Any(s => s.UserId == current.Id && s.ShipNumber == n)).ToList();
        dto.OwnShipCells = game.Ships.Where(s => s.UserId == current.Id).SelectMany(s => s.GetCells()
            .Select(c => new ShipCellDto { Row = c.Row, Column = c.Column, ShipNumber = s.ShipNumber })).ToList();
        dto.OwnAttacks = mapper.Map<List<AttackResultDto>>(game.Attacks.Where(a => a.UserId == current.Id).ToList());
        if (game.Status == BattleshipStatus.Finished)
            dto.OpponentAttacks = mapper.Map<List<AttackResultDto>>(game.Attacks.Where(a => a.UserId != current.Id).ToList());
        var hits = game.Attacks.Where(a => a.UserId == current.Id && a.IsHit)
            .Select(a => new BoardCoordinate(a.Row, a.Column)).ToHashSet();
        dto.OpponentShipsSunk = game.Ships.Count(s => s.UserId != current.Id && s.GetCells().All(hits.Contains));
        return dto;
    }

    /// <summary>Places a ship using a current form version and the authenticated participant identity.</summary>
    public Task PlaceShipAsync(int gameId, long expectedVersion, int shipNumber, int row, int column, ShipDirection direction)
        => MutateAsync(gameId, expectedVersion, (game, userId, now) => game.PlaceShip(userId, shipNumber, row, column, direction, now));

    /// <summary>Performs a current-turn attack and commits its result together with the next turn.</summary>
    public Task AttackAsync(int gameId, long expectedVersion, int row, int column)
        => MutateAsync(gameId, expectedVersion, (game, userId, now) => game.PerformAttack(userId, row, column, now));

    /// <summary>Finishes a participant's active or setup game after explicit confirmation in Web.</summary>
    public Task SurrenderAsync(int gameId, long expectedVersion)
        => MutateAsync(gameId, expectedVersion, (game, userId, now) => game.Surrender(userId, now));

    /// <summary>Applies one version-checked transition, giving automatic abandonment priority over late actions.</summary>
    private Task MutateAsync(int id, long version, Action<BattleshipGame, string, DateTime> action)
        => atomic.ExecuteAsync(async () =>
        {
            var current = await guard.GetCurrentAsync();
            var game = await GetOwnedGameAsync(id, current.Id);
            var now = clock.GetUtcNow().UtcDateTime;
            // A late action completes abandonment instead of changing a turn that has already expired.
            if (game.ExpireIfAbandoned(now)) return false;
            game.EnsureVersion(version);
            action(game, current.Id, now);
            return true;
        });

    /// <summary>Loads a complete aggregate before authorizing participant access.</summary>
    private async Task<BattleshipGame> GetOwnedGameAsync(int id, string userId)
    {
        var game = await games.GetGameAsync(id)
            ?? throw new DomainRuleException("La partida seleccionada no existe.");
        game.EnsureParticipant(userId);
        return game;
    }

    /// <summary>Builds history and active-game data without transmitting fleets or opponent attacks.</summary>
    private async Task<GameSummaryDto> ProjectSummaryAsync(BattleshipGame game, string userId)
    {
        var dto = mapper.Map<GameSummaryDto>(game);
        dto.Opponent = await users.GetByIdAsync(game.GetOpponentId(userId)) ?? new() { UserName = "Usuario no disponible" };
        dto.DurationHours = Math.Max(0, ((game.CompletedAt ?? clock.GetUtcNow().UtcDateTime) - game.CreatedAt).TotalHours);
        dto.IsWinner = game.WinnerUserId == userId;
        dto.WinnerUserName = game.WinnerUserId is null ? null : dto.IsWinner ? "Yo" : dto.Opponent.UserName;
        return dto;
    }
}
