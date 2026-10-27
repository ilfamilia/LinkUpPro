using LinkUpPro.Core.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Post
{
    public class UpdatePostViewModel
    {
        [Required(ErrorMessage = "Debe ingresar el contenido de la publicación.")]
        [StringLength(1000, ErrorMessage = "El contenido no puede superar los 1000 caracteres.")]
        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar el tipo de publicación.")]
        [EnumDataType(typeof(PostMediaType))]
        public PostMediaType? MediaType { get; set; }

        [DataType(DataType.Upload)]
        public IFormFile? ImageFile { get; set; }

        public string? CurrentImagePath { get; set; }

        [Url(ErrorMessage = "Debe ingresar una URL válida.")]
        [StringLength(2048, ErrorMessage = "La URL no puede superar los 2048 caracteres.")]
        public string? YouTubeUrl { get; set; }

        [EnumDataType(typeof(PostPrivacy))]
        public PostPrivacy Privacy { get; set; } = PostPrivacy.FriendsOnly;

        public bool AllowComments { get; set; } = true;
    }
}
