using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Battleship
{
    public class BattleshipAttackBoardDto
    {
        public int GameId { get; set; }
        public BattleshipGameStatus Status { get; set; }
        public bool IsMyTurn { get; set; }
        public string? CurrentTurnUserId { get; set; }
        public bool? DidIWin { get; set; }
        
        public List<BattleshipAttackCellDto> Attacks { get; set; } = [];
    }
}
