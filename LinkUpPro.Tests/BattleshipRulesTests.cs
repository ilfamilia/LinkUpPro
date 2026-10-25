using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Exceptions;
using Xunit;

namespace LinkUpPro.Tests;

/// <summary>Verifies fleet geometry, strict turn transitions, private participation, and terminal game rules.</summary>
public sealed class BattleshipRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Builds a playable aggregate with two valid separated fleets.</summary>
    internal static BattleshipGame ReadyGame(string first = "a", string second = "b")
    {
        var game = BattleshipGame.Create(first, second, Now);
        foreach (var user in new[] { first, second })
            for (var ship = 0; ship < 5; ship++)
                game.PlaceShip(user, ship, ship, 0, ShipDirection.Right, Now);
        return game;
    }

    [Fact]
    public void CreationOrdersPairWithoutChangingCreator()
    {
        var game = BattleshipGame.Create("z", "a", Now);
        Assert.Equal("a", game.User1Id);
        Assert.Equal("z", game.CreatorUserId);
    }

    [Theory]
    [InlineData("", "b")]
    [InlineData("a", "a")]
    [InlineData(" ", "b")]
    public void CreationRejectsInvalidParticipants(string first, string second)
        => Assert.Throws<DomainRuleException>(() => BattleshipGame.Create(first, second, Now));

    [Theory]
    [InlineData(0, 0, ShipDirection.Up)]
    [InlineData(0, 0, ShipDirection.Left)]
    [InlineData(11, 11, ShipDirection.Down)]
    [InlineData(11, 11, ShipDirection.Right)]
    public void PlacementRejectsShipsOutsideBoard(int row, int column, ShipDirection direction)
    {
        var game = BattleshipGame.Create("a", "b", Now);
        Assert.Throws<DomainRuleException>(() => game.PlaceShip("a", 4, row, column, direction, Now));
        Assert.Empty(game.Ships);
    }

    [Theory]
    [InlineData(5, 5, ShipDirection.Up)]
    [InlineData(5, 5, ShipDirection.Down)]
    [InlineData(5, 5, ShipDirection.Left)]
    [InlineData(5, 5, ShipDirection.Right)]
    public void PlacementAcceptsAllFourDirections(int row, int column, ShipDirection direction)
    {
        var game = BattleshipGame.Create("a", "b", Now);
        game.PlaceShip("a", 4, row, column, direction, Now);
        Assert.Equal(5, Assert.Single(game.Ships).GetCells().Count());
    }

    [Fact]
    public void PlacementRejectsOverlapAndRepeatedShip()
    {
        var game = BattleshipGame.Create("a", "b", Now);
        game.PlaceShip("a", 0, 0, 0, ShipDirection.Right, Now);
        Assert.Throws<DomainRuleException>(() => game.PlaceShip("a", 1, 0, 1, ShipDirection.Down, Now));
        Assert.Throws<DomainRuleException>(() => game.PlaceShip("a", 0, 5, 5, ShipDirection.Right, Now));
        Assert.Single(game.Ships);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(5, 0)]
    [InlineData(0, 44)]
    public void PlacementRejectsUnknownShipOrDirection(int ship, int direction)
        => Assert.Throws<DomainRuleException>(() => BattleshipGame.Create("a", "b", Now)
            .PlaceShip("a", ship, 0, 0, (ShipDirection)direction, Now));

    [Fact]
    public void SetupWaitsForBothFleetsAndCreatorStarts()
    {
        var game = BattleshipGame.Create("b", "a", Now);
        for (var ship = 0; ship < 5; ship++) game.PlaceShip("b", ship, ship, 0, ShipDirection.Right, Now);
        Assert.Equal(BattleshipStatus.Setup, game.Status);
        Assert.False(game.ExpireIfAbandoned(Now.AddDays(100)));
        for (var ship = 0; ship < 5; ship++) game.PlaceShip("a", ship, ship, 0, ShipDirection.Right, Now.AddHours(2));
        Assert.Equal("b", game.TurnUserId);
        Assert.Equal(Now.AddHours(2), game.TurnAssignedAt);
        Assert.Equal(17, game.Ships.Where(s => s.UserId == "a").Sum(s => s.Size));
    }

    [Fact]
    public void HitAndMissBothAlternateTurnsAndResetTurnClock()
    {
        var game = ReadyGame();
        Assert.True(game.PerformAttack("a", 0, 0, Now.AddHours(1)).IsHit);
        Assert.Equal("b", game.TurnUserId);
        Assert.False(game.PerformAttack("b", 11, 11, Now.AddHours(2)).IsHit);
        Assert.Equal("a", game.TurnUserId);
        Assert.Equal(Now.AddHours(2), game.TurnAssignedAt);
    }

    [Fact]
    public void AttackRejectsWrongTurnAndRepeatedCellWithoutConsumingTurn()
    {
        var game = ReadyGame();
        Assert.Throws<DomainRuleException>(() => game.PerformAttack("b", 0, 0, Now));
        game.PerformAttack("a", 0, 0, Now);
        game.PerformAttack("b", 11, 11, Now);
        var version = game.Version;
        Assert.Throws<DomainRuleException>(() => game.PerformAttack("a", 0, 0, Now));
        Assert.Equal(version, game.Version);
        Assert.Equal("a", game.TurnUserId);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(12, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 12)]
    public void AttackRejectsInvalidCoordinates(int row, int column)
        => Assert.Throws<DomainRuleException>(() => ReadyGame().PerformAttack("a", row, column, Now));

    [Fact]
    public void FullFleetDestructionEndsGameExactlyOnce()
    {
        var game = ReadyGame();
        var targets = game.Ships.Where(s => s.UserId == "b").SelectMany(s => s.GetCells()).ToList();
        for (var index = 0; index < targets.Count; index++)
        {
            game.PerformAttack("a", targets[index].Row, targets[index].Column, Now.AddMinutes(index * 2));
            if (index < targets.Count - 1)
                game.PerformAttack("b", 10 + index / 12, index % 12, Now.AddMinutes(index * 2 + 1));
        }
        Assert.Equal(BattleshipStatus.Finished, game.Status);
        Assert.Equal("a", game.WinnerUserId);
        Assert.Equal(GameEndReason.FleetDestroyed, game.EndReason);
        Assert.Null(game.TurnUserId);
        Assert.Throws<DomainRuleException>(() => game.Surrender("b", Now));
        Assert.Throws<DomainRuleException>(() => game.PerformAttack("a", 11, 11, Now));
    }

    [Fact]
    public void AbandonmentUsesCurrentTurnAndExactFortyEightHourBoundary()
    {
        var game = ReadyGame();
        game.PerformAttack("a", 0, 0, Now.AddHours(20));
        Assert.False(game.ExpireIfAbandoned(Now.AddHours(48)));
        Assert.False(game.ExpireIfAbandoned(Now.AddHours(68).AddTicks(-1)));
        Assert.True(game.ExpireIfAbandoned(Now.AddHours(68)));
        Assert.Equal("a", game.WinnerUserId);
        Assert.Equal(GameEndReason.Abandonment, game.EndReason);
        Assert.False(game.ExpireIfAbandoned(Now.AddHours(100)));
    }

    [Fact]
    public void SurrenderIsAllowedDuringSetupAndAwardsOpponent()
    {
        var game = BattleshipGame.Create("a", "b", Now);
        game.Surrender("a", Now.AddHours(2));
        Assert.Equal("b", game.WinnerUserId);
        Assert.Equal(GameEndReason.Surrender, game.EndReason);
        Assert.Equal(Now.AddHours(2), game.CompletedAt);
    }

    [Fact]
    public void ThirdPartyAndStaleFormsAreRejected()
    {
        var game = ReadyGame();
        Assert.Throws<DomainRuleException>(() => game.EnsureParticipant("c"));
        Assert.Throws<DomainRuleException>(() => game.PlaceShip("c", 0, 0, 0, ShipDirection.Right, Now));
        Assert.Throws<DomainRuleException>(() => game.PerformAttack("c", 0, 0, Now));
        Assert.Throws<DomainRuleException>(() => game.Surrender("c", Now));
        Assert.Throws<DomainRuleException>(() => game.EnsureVersion(game.Version - 1));
    }
}
