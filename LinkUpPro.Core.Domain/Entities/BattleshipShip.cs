using LinkUpPro.Core.Domain.Common;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Domain.Entities
{
    public class BattleshipShip : BasicEntity<int>
    {
        public required int GameId { get; set; }

        public required string UserId { get; set; }

        public required int ShipNumber { get; set; }

        public required int Size { get; set; }

        public required int StartRow { get; set; }

        public required int StartColumn { get; set; }

        public required BattleshipShipDirection Direction { get; set; }

        public BattleshipGame? Game { get; set; }
    }
}
