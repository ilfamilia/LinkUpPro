using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Battleship
{
    public class BattleshipResultDto
    {
        public int GameId { get; set; }
        public string OpponentUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public double DurationHours { get; set; }
        public bool DidIWin { get; set; }
        public BattleshipGameEndReason EndReason { get; set; }
        public List<BattleshipAttackCellDto> MyAttacks { get; set; } = [];
        public List<BattleshipAttackCellDto> OpponentAttacks { get; set; } = [];
    }
}
