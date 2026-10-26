using LinkUpPro.Core.Domain.Common;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Domain.Entities
{
    public class BattleshipGame : BasicEntity<int>
    {
        public required string CreatorUserId { get; set; }

        public required string OpponentUserId { get; set; }

        public required BattleshipGameStatus Status { get; set; }

        public string? CurrentTurnUserId { get; set; }

        public string? WinnerUserId { get; set; }

        public BattleshipGameEndReason? EndReason { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? BattleStartedAt { get; set; }

        public DateTime? TurnStartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public byte[] RowVersion { get; set; } = [];

        public ICollection<BattleshipShip> Ships { get; set; } = [];

        public ICollection<BattleshipAttack> Attacks { get; set; } = [];
    }
}
