using LinkUpPro.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinkUpPro.Core.Application.Dtos.Battleship
{
    public class BattleshipPlacementDto
    {
        public int GameId { get; set; }
        public BattleshipGameStatus Status { get; set; }
        public bool MyShipsReady { get; set; }
        public bool OpponentShipsReady { get; set; }
        public bool IsMyTurn { get; set; }
        public int PlacedShipCount { get; set; }
        public List<BattleshipShipOptionDto> RemainingShips { get; set; } = [];
        public List<BattleshipCellDto> OccupiedCells { get; set; } = [];
    }
}
