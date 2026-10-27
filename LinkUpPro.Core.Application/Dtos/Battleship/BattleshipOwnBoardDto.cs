using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Battleship
{
    public class BattleshipOwnBoardDto
    {
        public int GameId { get; set; }
        
        public BattleshipGameStatus Status { get; set; }
        
        public List<BattleshipCellDto> OccupiedCells { get; set; } = [];
        
        public List<BattleshipAttackCellDto> ReceivedAttacks { get; set; } = [];
    }
}
