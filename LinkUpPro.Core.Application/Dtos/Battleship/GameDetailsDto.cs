using LinkUpPro.Core.Domain.Entities;

namespace LinkUpPro.Core.Application.Dtos.Battleship;

/// <summary>Contains the caller's boards and reveals opponent attacks only after completion.</summary>
public sealed class GameDetailsDto : GameSummaryDto
{
    public bool IsOwnTurn { get; set; }
    public string? TurnUserName { get; set; }
    public DateTime? TurnAssignedAt { get; set; }
    public List<int> UnplacedShipNumbers { get; set; } = [];
    public List<ShipCellDto> OwnShipCells { get; set; } = [];
    public List<AttackResultDto> OwnAttacks { get; set; } = [];
    public List<AttackResultDto> OpponentAttacks { get; set; } = [];
    public int OpponentShipsSunk { get; set; }
}

/// <summary>Identifies an occupied cell of the caller's fleet.</summary>
public sealed class ShipCellDto
{
    public int Row { get; set; }
    public int Column { get; set; }
    public int ShipNumber { get; set; }
}

/// <summary>Transfers a recorded attack result without revealing untouched opponent cells.</summary>
public sealed class AttackResultDto
{
    public int Row { get; set; }
    public int Column { get; set; }
    public bool IsHit { get; set; }
}
