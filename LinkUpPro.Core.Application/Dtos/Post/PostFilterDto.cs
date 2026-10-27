using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Post
{
    public class PostFilterDto
    {
        public string? SearchText { get; set; }

        public PostMediaType? MediaType { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public bool? IsEdited { get; set; }

        public string? FriendUserId { get; set; }
    }
}
