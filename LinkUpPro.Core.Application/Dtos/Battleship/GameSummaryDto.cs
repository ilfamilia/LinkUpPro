using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Application.Dtos.Social;

namespace LinkUpPro.Core.Application.Dtos.Battleship;

/// <summary>Describes an active or completed game without exposing hidden boards.</summary>
public class GameSummaryDto
{
    public int Id { get; set; }
    public UserSummaryDto Opponent { get; set; } = new();
    public BattleshipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double DurationHours { get; set; }
    public bool IsWinner { get; set; }
    public string? WinnerUserName { get; set; }
    public GameEndReason? EndReason { get; set; }
    public long Version { get; set; }
}
