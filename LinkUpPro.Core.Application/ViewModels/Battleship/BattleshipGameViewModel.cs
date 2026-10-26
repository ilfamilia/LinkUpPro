using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.ViewModels.Battleship
{
    public class BattleshipGameViewModel
    {
        public int Id { get; set; }

        public string OpponentUserId { get; set; } = string.Empty;

        public string OpponentUserName { get; set; } = string.Empty;

        public string? OpponentProfileImage { get; set; }

        public BattleshipGameStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? BattleStartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public string? CurrentTurnUserId { get; set; }

        public string? WinnerUserId { get; set; }

        public BattleshipGameEndReason? EndReason { get; set; }

        public bool IsCreator { get; set; }

        public bool IsMyTurn { get; set; }

        public bool? DidIWin { get; set; }
    }
}
