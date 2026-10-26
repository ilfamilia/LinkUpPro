namespace LinkUpPro.Core.Application.ViewModels.Battleship
{
    public class BattleshipSelectCellViewModel
    {
        public BattleshipPlacementViewModel Placement { get; set; } = new();
        public int ShipNumber { get; set; }
        public int ShipSize { get; set; }
    }
}
