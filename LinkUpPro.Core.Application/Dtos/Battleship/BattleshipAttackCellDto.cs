using System;
using System.Collections.Generic;
using System.Text;

namespace LinkUpPro.Core.Application.Dtos.Battleship
{
    public class BattleshipAttackCellDto
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public bool IsHit { get; set; }
    }
}
