using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LinkUpPro.Tests;

/// <summary>Checks persisted gameplay, hidden-board projections, concurrent writes, and friendship independence.</summary>
[Collection("SqlDatabase")]
public sealed class BattleshipIntegrationTests(SqlDatabaseFixture database)
{
    [Fact]
    public async Task OnlyActiveFriendsCanStartOneUnfinishedGamePerPair()
    {
        await using var env = new TestEnvironment(database);
        var games = env.Get<IBattleshipService>();
        await Assert.ThrowsAsync<DomainRuleException>(() => games.CreateAsync(env.Bob));
        await Assert.ThrowsAsync<DomainRuleException>(() => games.CreateAsync(env.Inactive));
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        Assert.Equal(env.Bob, Assert.Single(await games.GetEligibleOpponentsAsync()).Id);
        await games.CreateAsync(env.Bob);
        await Assert.ThrowsAsync<DomainRuleException>(() => games.CreateAsync(env.Bob));
        Assert.Empty(await games.GetEligibleOpponentsAsync());
        env.Current.UserId = env.Bob;
        await Assert.ThrowsAsync<DomainRuleException>(() => games.CreateAsync(env.Alice));
    }

    [Fact]
    public async Task PlacementPersistsAcrossScopesAndDoesNotExposeOpponentShips()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var service = env.Get<IBattleshipService>();
        var id = await service.CreateAsync(env.Bob);
        var first = await service.GetGameAsync(id);
        await service.PlaceShipAsync(id, first.Version, 0, 0, 0, ShipDirection.Right);
        await using var other = new TestEnvironment(database, env.Directory, env.Bob, env.Clock);
        var opponentView = await other.Get<IBattleshipService>().GetGameAsync(id);
        Assert.Empty(opponentView.OwnShipCells);
        Assert.Empty(opponentView.OpponentAttacks);
        Assert.Equal(5, opponentView.UnplacedShipNumbers.Count);
        await using var resumed = new TestEnvironment(database, env.Directory, env.Alice, env.Clock);
        Assert.Equal(2, (await resumed.Get<IBattleshipService>().GetGameAsync(id)).OwnShipCells.Count);
    }

    [Fact]
    public async Task ThirdPartyCannotReadMutateOrSurrenderAnyGame()
    {
        await using var env = new TestEnvironment(database);
        var game = await SeedReadyGameAsync(env);
        env.Current.UserId = env.Charlie;
        var service = env.Get<IBattleshipService>();
        await Assert.ThrowsAsync<DomainRuleException>(() => service.GetGameAsync(game.Id));
        await Assert.ThrowsAsync<DomainRuleException>(() => service.AttackAsync(game.Id, game.Version, 0, 0));
        await Assert.ThrowsAsync<DomainRuleException>(() => service.SurrenderAsync(game.Id, game.Version));
        Assert.Empty(await service.GetGamesAsync());
    }

    [Fact]
    public async Task AttacksPersistAndOldFormsCannotAttackAgainOnALaterTurn()
    {
        await using var env = new TestEnvironment(database);
        var game = await SeedReadyGameAsync(env);
        var service = env.Get<IBattleshipService>();
        var initialVersion = game.Version;
        await service.AttackAsync(game.Id, initialVersion, 0, 0);
        env.Current.UserId = env.Bob;
        var opponent = await service.GetGameAsync(game.Id);
        Assert.True(opponent.IsOwnTurn);
        Assert.Empty(opponent.OpponentAttacks);
        await service.AttackAsync(game.Id, opponent.Version, 11, 11);
        env.Current.UserId = env.Alice;
        await Assert.ThrowsAsync<DomainRuleException>(() => service.AttackAsync(game.Id, initialVersion, 0, 1));
        var current = await service.GetGameAsync(game.Id);
        Assert.True(current.IsOwnTurn);
        Assert.Single(current.OwnAttacks);
        Assert.Empty(current.OpponentAttacks);
    }

    [Fact]
    public async Task RemovingFriendshipPreservesCurrentGameButBlocksNewOnes()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var game = await SeedReadyGameAsync(env);
        await env.Get<IFriendshipService>().RemoveAsync(env.Bob);
        var service = env.Get<IBattleshipService>();
        await service.AttackAsync(game.Id, game.Version, 0, 0);
        var details = await service.GetGameAsync(game.Id);
        await service.SurrenderAsync(game.Id, details.Version);
        await Assert.ThrowsAsync<DomainRuleException>(() => service.CreateAsync(env.Bob));
        var history = Assert.Single(await service.GetGamesAsync());
        Assert.Equal(BattleshipStatus.Finished, history.Status);
        Assert.False(history.IsWinner);
    }

    [Fact]
    public async Task ExpiredTurnFinishesOnAccessAndLateAttackDoesNotChangeTheOutcome()
    {
        await using var env = new TestEnvironment(database);
        var game = await SeedReadyGameAsync(env);
        env.Clock.Now = env.Clock.Now.AddHours(48);
        var service = env.Get<IBattleshipService>();
        await service.AttackAsync(game.Id, game.Version, 0, 0);
        var details = await service.GetGameAsync(game.Id);
        Assert.Equal(BattleshipStatus.Finished, details.Status);
        Assert.Equal(GameEndReason.Abandonment, details.EndReason);
        Assert.False(details.IsWinner);
        Assert.Empty(details.OwnAttacks);
        Assert.Equal(48, details.DurationHours);
        await Assert.ThrowsAsync<DomainRuleException>(() => service.SurrenderAsync(game.Id, details.Version));
    }

    [Fact]
    public async Task SetupNeverExpiresAndFinishedResultsRevealOnlyAuthorizedAttackLogs()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var service = env.Get<IBattleshipService>();
        var setupId = await service.CreateAsync(env.Bob);
        env.Clock.Now = env.Clock.Now.AddDays(100);
        Assert.Equal(BattleshipStatus.Setup, (await service.GetGameAsync(setupId)).Status);
        var setup = await service.GetGameAsync(setupId);
        await service.SurrenderAsync(setupId, setup.Version);
        var game = await SeedReadyGameAsync(env);
        await service.AttackAsync(game.Id, game.Version, 0, 0);
        env.Current.UserId = env.Bob;
        var current = await service.GetGameAsync(game.Id);
        await service.SurrenderAsync(game.Id, current.Version);
        var result = await service.GetGameAsync(game.Id);
        Assert.Single(result.OpponentAttacks);
        Assert.Empty(result.OwnAttacks);
        Assert.Equal(17, result.OwnShipCells.Count);
    }

    [Fact]
    public async Task ConcurrentGameCreationCannotCreateDuplicateActivePairs()
    {
        await using var seed = new TestEnvironment(database);
        await seed.MakeFriendsAsync(seed.Alice, seed.Bob);
        await using var first = new TestEnvironment(database, seed.Directory, seed.Alice, seed.Clock);
        await using var second = new TestEnvironment(database, seed.Directory, seed.Bob, seed.Clock);
        var outcomes = await Task.WhenAll(
            FriendshipIntegrationTests.AttemptAsync(async () => { await first.Get<IBattleshipService>().CreateAsync(seed.Bob); }),
            FriendshipIntegrationTests.AttemptAsync(async () => { await second.Get<IBattleshipService>().CreateAsync(seed.Alice); }));
        Assert.Equal(1, outcomes.Count(success => success));
        await using var check = database.CreateContext();
        Assert.Single(await check.BattleshipGames.Where(g => g.User1Id == seed.Alice && g.User2Id == seed.Bob
            && g.Status != BattleshipStatus.Finished).ToListAsync());
    }

    [Fact]
    public async Task ConcurrentAttacksConsumeOnlyOneTurn()
    {
        await using var seed = new TestEnvironment(database);
        var game = await SeedReadyGameAsync(seed);
        await using var first = new TestEnvironment(database, seed.Directory, seed.Alice, seed.Clock);
        await using var second = new TestEnvironment(database, seed.Directory, seed.Alice, seed.Clock);
        var outcomes = await Task.WhenAll(
            FriendshipIntegrationTests.AttemptAsync(() => first.Get<IBattleshipService>().AttackAsync(game.Id, game.Version, 0, 0)),
            FriendshipIntegrationTests.AttemptAsync(() => second.Get<IBattleshipService>().AttackAsync(game.Id, game.Version, 0, 1)));
        Assert.Equal(1, outcomes.Count(success => success));
        await using var check = database.CreateContext();
        Assert.Equal(seed.Bob, (await check.BattleshipGames.FindAsync(game.Id))!.TurnUserId);
        Assert.Single(await check.Set<Attack>().Where(a => a.GameId == game.Id).ToListAsync());
    }

    [Fact]
    public async Task ConcurrentPlacementCannotPlaceTheSameShipTwice()
    {
        await using var seed = new TestEnvironment(database);
        await seed.MakeFriendsAsync(seed.Alice, seed.Bob);
        var id = await seed.Get<IBattleshipService>().CreateAsync(seed.Bob);
        var game = await seed.Get<IBattleshipService>().GetGameAsync(id);
        await using var first = new TestEnvironment(database, seed.Directory, seed.Alice, seed.Clock);
        await using var second = new TestEnvironment(database, seed.Directory, seed.Alice, seed.Clock);
        var outcomes = await Task.WhenAll(
            FriendshipIntegrationTests.AttemptAsync(() => first.Get<IBattleshipService>().PlaceShipAsync(id, game.Version, 0, 0, 0, ShipDirection.Right)),
            FriendshipIntegrationTests.AttemptAsync(() => second.Get<IBattleshipService>().PlaceShipAsync(id, game.Version, 0, 3, 3, ShipDirection.Down)));
        Assert.Equal(1, outcomes.Count(success => success));
        await using var check = database.CreateContext();
        Assert.Single(await check.Set<ShipPlacement>().Where(s => s.GameId == id && s.UserId == seed.Alice).ToListAsync());
    }

    [Fact]
    public async Task ConcurrentSurrenderCannotFinishTheGameTwice()
    {
        await using var seed = new TestEnvironment(database);
        var game = await SeedReadyGameAsync(seed);
        await using var first = new TestEnvironment(database, seed.Directory, seed.Alice, seed.Clock);
        await using var second = new TestEnvironment(database, seed.Directory, seed.Bob, seed.Clock);
        var outcomes = await Task.WhenAll(
            FriendshipIntegrationTests.AttemptAsync(() => first.Get<IBattleshipService>().SurrenderAsync(game.Id, game.Version)),
            FriendshipIntegrationTests.AttemptAsync(() => second.Get<IBattleshipService>().SurrenderAsync(game.Id, game.Version)));
        Assert.Equal(1, outcomes.Count(success => success));
        await using var check = database.CreateContext();
        var stored = await check.BattleshipGames.FindAsync(game.Id);
        Assert.Equal(BattleshipStatus.Finished, stored!.Status);
        Assert.Equal(game.Version + 1, stored.Version);
    }

    [Fact]
    public async Task ExpiredPairCanStartANewGameWithoutViolatingTheActivePairIndex()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var game = await SeedReadyGameAsync(env);
        env.Clock.Now = env.Clock.Now.AddHours(48);
        var service = env.Get<IBattleshipService>();
        var replacement = await service.CreateAsync(env.Bob.ToUpperInvariant());
        Assert.NotEqual(game.Id, replacement);
        var list = await service.GetGamesAsync();
        Assert.Single(list, g => g.Status == BattleshipStatus.Setup);
        Assert.Single(list, g => g.EndReason == GameEndReason.Abandonment);
    }

    [Fact]
    public async Task FullGamePersistsFleetVictoryAndReadOnlyFinalBoards()
    {
        await using var env = new TestEnvironment(database);
        var game = await SeedReadyGameAsync(env);
        var service = env.Get<IBattleshipService>();
        var targets = game.Ships.Where(s => s.UserId == env.Bob).SelectMany(s => s.GetCells()).ToList();
        for (var index = 0; index < targets.Count; index++)
        {
            env.Current.UserId = env.Alice;
            var current = await service.GetGameAsync(game.Id);
            await service.AttackAsync(game.Id, current.Version, targets[index].Row, targets[index].Column);
            if (index == targets.Count - 1) break;
            env.Current.UserId = env.Bob;
            current = await service.GetGameAsync(game.Id);
            await service.AttackAsync(game.Id, current.Version, 10 + index / 12, index % 12);
        }
        var result = await service.GetGameAsync(game.Id);
        Assert.Equal(BattleshipStatus.Finished, result.Status);
        Assert.True(result.IsWinner);
        Assert.Equal(5, result.OpponentShipsSunk);
        Assert.Equal(17, result.OwnAttacks.Count);
        Assert.Equal(16, result.OpponentAttacks.Count);
        await Assert.ThrowsAsync<DomainRuleException>(() => service.AttackAsync(game.Id, result.Version, 11, 11));
    }

    /// <summary>Seeds a complete persisted aggregate to focus each test on a specific application transition.</summary>
    private static async Task<BattleshipGame> SeedReadyGameAsync(TestEnvironment env)
    {
        var game = BattleshipRulesTests.ReadyGame(env.Alice, env.Bob);
        // Keep seeded combat aligned with the test clock rather than the domain fixture's fixed timestamp.
        game.CreatedAt = env.Clock.Now.UtcDateTime;
        game.TurnAssignedAt = env.Clock.Now.UtcDateTime;
        env.Context.BattleshipGames.Add(game);
        await env.Context.SaveChangesAsync();
        return game;
    }
}
