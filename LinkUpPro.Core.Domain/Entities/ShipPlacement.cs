using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Domain.Entities;

/// <summary>Stores the starting cell and direction of one player's numbered ship.</summary>
public sealed class ShipPlacement
{
    public int GameId { get; set; }
    public string UserId { get; set; } = "";
    public int ShipNumber { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public ShipDirection Direction { get; set; }
    public int Size { get; set; }

    /// <summary>Expands the placement into its occupied cells without accessing persistence.</summary>
    public IEnumerable<BoardCoordinate> GetCells()
    {
        var (rowStep, columnStep) = Direction switch
        {
            ShipDirection.Up => (-1, 0),
            ShipDirection.Down => (1, 0),
            ShipDirection.Left => (0, -1),
            _ => (0, 1)
        };
        for (var offset = 0; offset < Size; offset++)
            yield return new(Row + rowStep * offset, Column + columnStep * offset);
    }
}

