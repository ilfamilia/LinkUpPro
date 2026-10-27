using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Battleship
{
    public class BattleshipGameDto
    {
        public int Id { get; set; }

        public required string OpponentUserId { get; set; }

        public required string OpponentUserName { get; set; }

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
