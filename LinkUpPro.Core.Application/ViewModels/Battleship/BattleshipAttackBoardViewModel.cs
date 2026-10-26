using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.ViewModels.Battleship
{
    public class BattleshipAttackBoardViewModel
    {
        public int GameId { get; set; }
        public BattleshipGameStatus Status { get; set; }
        public bool IsMyTurn { get; set; }
        public string? CurrentTurnUserId { get; set; }
        public bool? DidIWin { get; set; }
        public List<BattleshipAttackCellViewModel> Attacks { get; set; } = [];
    }
}
