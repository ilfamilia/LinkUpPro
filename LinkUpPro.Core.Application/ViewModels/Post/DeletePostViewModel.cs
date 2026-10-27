using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Post
{
    public class DeletePostViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "El ID de la publicación es inválido.")]
        public int Id { get; set; }
    }
}
