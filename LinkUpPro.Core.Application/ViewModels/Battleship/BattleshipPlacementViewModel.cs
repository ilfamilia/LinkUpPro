using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.ViewModels.Battleship
{
    public class BattleshipPlacementViewModel
    {
        public int GameId { get; set; }
        public BattleshipGameStatus Status { get; set; }
        public bool MyShipsReady { get; set; }
        public bool OpponentShipsReady { get; set; }
        public bool IsMyTurn { get; set; }
        public int PlacedShipCount { get; set; }
        public List<BattleshipShipOptionViewModel> RemainingShips { get; set; } = [];
        public List<BattleshipCellViewModel> OccupiedCells { get; set; } = [];
    }
}
