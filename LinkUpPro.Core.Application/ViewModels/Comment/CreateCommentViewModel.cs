using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Comment
{
    public class CreateCommentViewModel
    {
        [Required(ErrorMessage = "Debe ingresar el contenido del comentario.")]
        [StringLength(500, ErrorMessage = "El comentario no puede superar los 500 caracteres.")]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "El ID de la publicación es inválido.")]
        public int PostId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El ID del comentario padre es inválido.")]
        public int? ParentCommentId { get; set; }

        public string? ReturnUrl { get; set; }
    }
}