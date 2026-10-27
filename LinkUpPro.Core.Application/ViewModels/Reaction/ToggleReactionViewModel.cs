using LinkUpPro.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Reaction
{
    public class ToggleReactionViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "El ID de la publicación es inválido.")]
        public int PostId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar una reacción.")]
        [EnumDataType(typeof(ReactionType), ErrorMessage = "El tipo de reacción es inválido.")]
        public ReactionType? ReactionType { get; set; }
    }
}
