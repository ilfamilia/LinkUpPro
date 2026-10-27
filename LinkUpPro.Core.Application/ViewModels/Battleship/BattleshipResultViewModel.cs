using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.ViewModels.Battleship
{
    public class BattleshipResultViewModel
    {
        public int GameId { get; set; }
        public string OpponentUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public double DurationHours { get; set; }
        public bool DidIWin { get; set; }
        public BattleshipGameEndReason EndReason { get; set; }
        public bool ShowOpponentBoard { get; set; }
        public List<BattleshipAttackCellViewModel> MyAttacks { get; set; } = [];
        public List<BattleshipAttackCellViewModel> OpponentAttacks { get; set; } = [];
    }
}
