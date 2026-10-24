using LinkUpPro.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Post
{
    public class PostFilterViewModel
    {
        [StringLength(1000, ErrorMessage = "El texto de búsqueda no puede superar los 1000 caracteres")]
        public string? SearchText { get; set; }

        [EnumDataType(typeof(PostMediaType))]
        public PostMediaType? MediaType { get; set; }

        [DataType(DataType.Date)]
        public DateOnly? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateOnly? EndDate { get; set; }

        public bool? IsEdited { get; set; }
    }
}
