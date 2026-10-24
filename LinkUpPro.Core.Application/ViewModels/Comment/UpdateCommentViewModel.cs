using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Comment
{
    public class UpdateCommentViewModel
    {
        [Required(ErrorMessage = "Debe ingresar el contenido del comentario")]
        [StringLength(500, ErrorMessage = "El comentario no puede superar los 500 caracteres")]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = string.Empty;
    }
}