namespace LinkUpPro.Core.Application.Dtos.Battleship
{
    public class BattleshipGameListDto
    {
        public int TotalGames { get; set; }
        public int WonGames { get; set; }
        public int LostGames { get; set; }

        public List<BattleshipGameDto> ActiveGames { get; set; } = [];

        public List<BattleshipGameDto> FinishedGames { get; set; } = [];
    }
}
