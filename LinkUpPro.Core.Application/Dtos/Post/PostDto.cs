using LinkUpPro.Core.Application.Dtos.Common;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Post
{
    public class PostDto : BasicDto<int>
    {
        // User information
        public required string UserId { get; set; }

        // Post information
        public required string Content { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public PostPrivacy Privacy { get; set; }

        public bool AllowComments { get; set; }

        // Media information
        public PostMediaType MediaType { get; set; }

        public string? ImagePath { get; set; }

        public string? YouTubeUrl { get; set; }

        public string? YouTubeEmbedUrl { get; set; }

        // Reactions information
        public int LikesCount { get; set; }

        public int DislikesCount { get; set; }

        public ReactionType? CurrentUserReaction { get; set; }

        // Comments information
        public int CommentsCount { get; set; }
    }
}