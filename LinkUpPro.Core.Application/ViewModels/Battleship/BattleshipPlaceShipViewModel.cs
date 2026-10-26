using LinkUpPro.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Battleship
{
    public class BattleshipPlaceShipViewModel
    {
        [Range(1, int.MaxValue)]
        public int GameId { get; set; }

        [Range(1, 5, ErrorMessage = "Debe seleccionar un barco válido.")]
        public int ShipNumber { get; set; }

        [Range(0, 11, ErrorMessage = "La fila debe estar entre 1 y 12.")]
        public int Row { get; set; }

        [Range(0, 11, ErrorMessage = "La columna debe estar entre 1 y 12.")]
        public int Column { get; set; }

        [Required(ErrorMessage = "Seleccione una dirección.")]
        public BattleshipShipDirection? Direction { get; set; }
    }
}
