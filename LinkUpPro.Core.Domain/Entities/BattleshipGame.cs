using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Exceptions;

namespace LinkUpPro.Core.Domain.Entities;

/// <summary>Owns fleet placement, turn changes, victory, surrender, and abandonment rules.</summary>
public sealed class BattleshipGame
{
    private static readonly int[] FleetSizes = [2, 3, 3, 4, 5];
    public int Id { get; set; }
    public string User1Id { get; set; } = "";
    public string User2Id { get; set; } = "";
    public string CreatorUserId { get; set; } = "";
    public BattleshipStatus Status { get; set; }
    public string? TurnUserId { get; set; }
    public DateTime? TurnAssignedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? WinnerUserId { get; set; }
    public GameEndReason? EndReason { get; set; }
    public long Version { get; set; } = 1;
    public ICollection<ShipPlacement> Ships { get; set; } = new List<ShipPlacement>();
    public ICollection<Attack> Attacks { get; set; } = new List<Attack>();

    /// <summary>Creates a canonical user pair while preserving the player who starts combat.</summary>
    public static BattleshipGame Create(string creatorId, string opponentId, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(creatorId) || string.IsNullOrWhiteSpace(opponentId) || creatorId == opponentId)
            throw new DomainRuleException("Debe seleccionar un oponente diferente.");
        var ordered = string.CompareOrdinal(creatorId, opponentId) < 0;
        return new() { User1Id = ordered ? creatorId : opponentId, User2Id = ordered ? opponentId : creatorId,
            CreatorUserId = creatorId, CreatedAt = now, Status = BattleshipStatus.Setup };
    }

    /// <summary>Rejects access from users who are not participants.</summary>
    public void EnsureParticipant(string userId)
    {
        if (userId != User1Id && userId != User2Id)
            throw new DomainRuleException("No posee permisos para acceder a esta partida.");
    }

    /// <summary>Returns the other participant after verifying membership.</summary>
    public string GetOpponentId(string userId)
    {
        EnsureParticipant(userId);
        return userId == User1Id ? User2Id : User1Id;
    }

    /// <summary>Rejects stale forms so a repeated submission cannot consume a later turn.</summary>
    public void EnsureVersion(long expectedVersion)
    {
        if (expectedVersion != Version)
            throw new DomainRuleException("La partida cambió. Refresque la pantalla e inténtelo nuevamente.");
    }

    /// <summary>Places one numbered ship and starts combat only when both fleets are complete.</summary>
    public void PlaceShip(string userId, int shipNumber, int row, int column, ShipDirection direction, DateTime now)
    {
        EnsureParticipant(userId);
        if (Status != BattleshipStatus.Setup)
            throw new DomainRuleException("La configuración de barcos ya finalizó.");
        if (shipNumber < 0 || shipNumber >= FleetSizes.Length || !Enum.IsDefined(direction))
            throw new DomainRuleException("Debe seleccionar un barco y una dirección válidos.");
        if (Ships.Any(s => s.UserId == userId && s.ShipNumber == shipNumber))
            throw new DomainRuleException("Este barco ya fue posicionado.");
        var ship = new ShipPlacement { GameId = Id, UserId = userId, ShipNumber = shipNumber,
            Row = row, Column = column, Direction = direction, Size = FleetSizes[shipNumber] };
        var cells = ship.GetCells().ToList();
        if (cells.Any(c => !c.IsInside()))
            throw new DomainRuleException("La combinación actual no es válida: parte del barco quedaría fuera del tablero. Debe cambiar la celda seleccionada o la dirección.");
        var occupied = Ships.Where(s => s.UserId == userId).SelectMany(s => s.GetCells()).ToHashSet();
        if (cells.Any(occupied.Contains))
            throw new DomainRuleException("Debe cambiar la celda seleccionada o la dirección, ya que con la combinación actual el barco quedaría posicionado encima de otro barco.");
        Ships.Add(ship);
        if (Ships.Count(s => s.UserId == User1Id) == 5 && Ships.Count(s => s.UserId == User2Id) == 5)
        {
            Status = BattleshipStatus.Active;
            TurnUserId = CreatorUserId;
            TurnAssignedAt = now;
        }
        Version++;
    }

    /// <summary>Records one attack and atomically determines victory or the next player's turn.</summary>
    public Attack PerformAttack(string userId, int row, int column, DateTime now)
    {
        EnsureParticipant(userId);
        if (Status != BattleshipStatus.Active || TurnUserId != userId)
            throw new DomainRuleException("No es su turno de atacar.");
        var cell = new BoardCoordinate(row, column);
        if (!cell.IsInside())
            throw new DomainRuleException("La celda seleccionada no es válida.");
        if (Attacks.Any(a => a.UserId == userId && a.Row == row && a.Column == column))
            throw new DomainRuleException("Esta celda ya fue atacada.");
        var opponent = GetOpponentId(userId);
        var targetCells = Ships.Where(s => s.UserId == opponent).SelectMany(s => s.GetCells()).ToHashSet();
        var attack = new Attack { GameId = Id, UserId = userId, Row = row, Column = column,
            IsHit = targetCells.Contains(cell), CreatedAt = now };
        Attacks.Add(attack);
        var hits = Attacks.Where(a => a.UserId == userId && a.IsHit)
            .Select(a => new BoardCoordinate(a.Row, a.Column)).ToHashSet();
        if (targetCells.Count == 17 && targetCells.All(hits.Contains))
            Finish(userId, GameEndReason.FleetDestroyed, now);
        else
        {
            TurnUserId = opponent;
            TurnAssignedAt = now;
            Version++;
        }
        return attack;
    }

    /// <summary>Ends a noncompleted game and awards victory to the participant who did not surrender.</summary>
    public void Surrender(string userId, DateTime now)
    {
        EnsureParticipant(userId);
        if (Status == BattleshipStatus.Finished)
            throw new DomainRuleException("La partida ya finalizó.");
        Finish(GetOpponentId(userId), GameEndReason.Surrender, now);
    }

    /// <summary>Expires only a combat turn that has been waiting for at least forty-eight hours.</summary>
    public bool ExpireIfAbandoned(DateTime now)
    {
        if (Status != BattleshipStatus.Active || TurnAssignedAt is null || TurnUserId is null
            || now - TurnAssignedAt.Value < TimeSpan.FromHours(48))
            return false;
        Finish(GetOpponentId(TurnUserId), GameEndReason.Abandonment, now);
        return true;
    }

    /// <summary>Applies the terminal transition once and clears the active turn.</summary>
    private void Finish(string winnerId, GameEndReason reason, DateTime now)
    {
        Status = BattleshipStatus.Finished;
        WinnerUserId = winnerId;
        EndReason = reason;
        CompletedAt = now;
        TurnUserId = null;
        TurnAssignedAt = null;
        Version++;
    }
}

