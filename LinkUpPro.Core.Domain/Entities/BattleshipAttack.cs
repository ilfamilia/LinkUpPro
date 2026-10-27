using LinkUpPro.Core.Domain.Common;

namespace LinkUpPro.Core.Domain.Entities
{
    public class BattleshipAttack : BasicEntity<int>
    {
        public required int GameId { get; set; }

        public required string AttackerUserId { get; set; }

        public required int Row { get; set; }

        public required int Column { get; set; }

        public required bool IsHit { get; set; }

        public required DateTime AttackedAt { get; set; }

        public BattleshipGame? Game { get; set; }
    }
}
