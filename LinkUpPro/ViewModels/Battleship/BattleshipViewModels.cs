using System.ComponentModel.DataAnnotations;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.ViewModels.Social;

namespace LinkUpPro.ViewModels.Battleship;

/// <summary>Displays the safe summary shared by active games and historical results.</summary>
public class GameSummaryViewModel
{
    public int Id { get; set; }
    public PublicUserViewModel Opponent { get; set; } = new();
    public BattleshipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double DurationHours { get; set; }
    public bool IsWinner { get; set; }
    public string? WinnerUserName { get; set; }
    public GameEndReason? EndReason { get; set; }
    public long Version { get; set; }
}

/// <summary>Separates active games from completed results and their statistics.</summary>
public sealed class BattleshipIndexViewModel
{
    public List<GameSummaryViewModel> Active { get; set; } = [];
    public List<GameSummaryViewModel> History { get; set; } = [];
    public int Wins => History.Count(g => g.IsWinner);
    public int Losses => History.Count - Wins;
}

/// <summary>Validates one opponent selection independently from the account directory.</summary>
public sealed class NewGameViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un amigo para iniciar la partida.")]
    [StringLength(36)]
    public string? OpponentId { get; set; }
    public string? Search { get; set; }
    public List<PublicUserViewModel> Candidates { get; set; } = [];
}

/// <summary>Transports a resource identifier and expected version without exposing participants or results.</summary>
public class GameActionViewModel
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
    [Range(typeof(long), "1", "9223372036854775807")]
    public long Version { get; set; }
}

/// <summary>Validates the ship, coordinate, and direction chosen during setup.</summary>
public sealed class PlaceShipViewModel : GameActionViewModel
{
    [Range(0, 4)]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
    public int ShipNumber { get; set; }
    [Range(0, 11)]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
    public int Row { get; set; }
    [Range(0, 11)]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
    public int Column { get; set; }
    [Required(ErrorMessage = "Debe seleccionar una dirección.")]
    [EnumDataType(typeof(ShipDirection))]
    public ShipDirection? Direction { get; set; }

    /// <summary>Display-only preview of the caller's fleet; reloaded on the server, never trusted from the form.</summary>
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public List<ShipCellViewModel> OwnShipCells { get; set; } = [];
}

/// <summary>Binds only the desired attack cell; the service calculates the result and next turn.</summary>
public sealed class AttackViewModel : GameActionViewModel
{
    [Range(0, 11)]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
    public int Row { get; set; }
    [Range(0, 11)]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired]
    public int Column { get; set; }
}

/// <summary>Identifies a visible cell in the caller's own fleet.</summary>
public sealed class ShipCellViewModel
{
    public int Row { get; set; }
    public int Column { get; set; }
    public int ShipNumber { get; set; }
}

/// <summary>Displays an authorized historical attack result.</summary>
public sealed class AttackCellViewModel
{
    public int Row { get; set; }
    public int Column { get; set; }
    public bool IsHit { get; set; }
}

/// <summary>Contains only boards and actions authorized for the current game participant.</summary>
public sealed class GameDetailsViewModel : GameSummaryViewModel
{
    public bool IsOwnTurn { get; set; }
    public string? TurnUserName { get; set; }
    public DateTime? TurnAssignedAt { get; set; }
    public List<int> UnplacedShipNumbers { get; set; } = [];
    public List<ShipCellViewModel> OwnShipCells { get; set; } = [];
    public List<AttackCellViewModel> OwnAttacks { get; set; } = [];
    public List<AttackCellViewModel> OpponentAttacks { get; set; } = [];
    public int OpponentShipsSunk { get; set; }
    public int SelectedShipNumber { get; set; }
    public bool ShowOwnBoard { get; set; }
    public bool ShowOpponentAttacks { get; set; }
}

/// <summary>Visual state of one board cell.</summary>
public enum BoardCellState { Water, Ship, Hit, Miss, Selected }

/// <summary>How a cell reacts to a click: nothing, a navigation link, or a protected attack form.</summary>
public enum BoardCellMode { None, Link, Attack }

/// <summary>One of the 144 cells rendered by the shared 12x12 board partial.</summary>
public sealed class BoardCellViewModel
{
    public int Row { get; set; }
    public int Column { get; set; }
    public BoardCellState State { get; set; }
    public BoardCellMode Mode { get; set; }
    public string? Href { get; set; }
}

/// <summary>Input for the shared board partial; attack cells post the game id and expected version.</summary>
public sealed class BoardViewModel
{
    public string Title { get; set; } = "";
    public int GameId { get; set; }
    public long Version { get; set; }
    public List<BoardCellViewModel> Cells { get; set; } = [];

    /// <summary>Creates the 144 cells in row-major order using a state/mode selector.</summary>
    public static BoardViewModel Create(string title, int gameId, long version,
        Func<int, int, (BoardCellState State, BoardCellMode Mode, string? Href)> cell)
    {
        var board = new BoardViewModel { Title = title, GameId = gameId, Version = version };
        for (var row = 0; row < 12; row++)
            for (var column = 0; column < 12; column++)
            {
                var (state, mode, href) = cell(row, column);
                board.Cells.Add(new() { Row = row, Column = column, State = state, Mode = mode, Href = href });
            }
        return board;
    }
}
