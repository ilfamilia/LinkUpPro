namespace LinkUpPro.Core.Application.ViewModels.Battleship
{
    public class BattleshipIndexViewModel
    {
        public int TotalGames { get; set; }
        public int WonGames { get; set; }
        public int LostGames { get; set; }

        public List<BattleshipGameViewModel> ActiveGames { get; set; } = [];

        public List<BattleshipGameViewModel> FinishedGames { get; set; } = [];
    }
}
