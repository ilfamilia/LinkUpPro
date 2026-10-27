namespace LinkUpPro.Core.Application.ViewModels.Battleship
{
    public class BattleshipSurrenderViewModel
    {
        public int GameId { get; set; }
        public string OpponentUserName { get; set; } = string.Empty;
        public string CancelAction { get; set; } = "AttackBoard";
    }
}
