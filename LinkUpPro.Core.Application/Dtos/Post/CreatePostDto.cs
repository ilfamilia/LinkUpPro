using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Post
{
    public class CreatePostDto
    {
        public required string UserId { get; set; }

        public required string Content { get; set; }

        public PostMediaType MediaType { get; set; }

        public string? ImagePath { get; set; }

        public string? YouTubeUrl { get; set; }

        public PostPrivacy Privacy { get; set; } = PostPrivacy.FriendsOnly;

        public bool AllowComments { get; set; } = true;
    }
}
