using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Battleship
{
    public class BattleshipAttackViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "La partida no es válida.")]
        public int GameId { get; set; }

        [Range(0, 11, ErrorMessage = "La fila debe estar entre 1 y 12.")]
        public int Row { get; set; }

        [Range(0, 11, ErrorMessage = "La columna debe estar entre 1 y 12.")]
        public int Column { get; set; }
    }
}
