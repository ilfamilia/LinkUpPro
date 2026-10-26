using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.ViewModels.Battleship
{
    public class BattleshipOwnBoardViewModel
    {
        public int GameId { get; set; }
        public BattleshipGameStatus Status { get; set; }
        public List<BattleshipCellViewModel> OccupiedCells { get; set; } = [];
        public List<BattleshipAttackCellViewModel> ReceivedAttacks { get; set; } = [];
    }
}
