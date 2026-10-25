using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Domain.Queries
{
    /// <summary>Combinable filters for the author's own posts; null values are ignored.</summary>
    public class PostSearchCriteria
    {
        public string? SearchText { get; set; }
        public PostMediaType? MediaType { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public bool? IsEdited { get; set; }
    }
}
