using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Battleship
{
    public class NewBattleshipGameViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un amigo para iniciar una partida.")]
        public string? SelectedOpponentUserId { get; set; }

        public string? SearchText { get; set; }

        public List<BattleshipOpponentViewModel> Opponents { get; set; } = [];
    }
}
