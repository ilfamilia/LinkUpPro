using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Comment
{
    public class DeleteCommentViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "El ID del comentario es inválido.")]
        public int Id { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
