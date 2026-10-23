using LinkUpPro.Core.Application.ViewModels.Comment;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.ViewModels.Post
{
    public class PostViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserProfilePicturePath { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public PostPrivacy Privacy { get; set; }
        public PostMediaType MediaType { get; set; }
        public string? ImagePath { get; set; }
        public string? YouTubeUrl { get; set; }
        public string? YouTubeEmbedUrl { get; set; }
        public bool AllowComments { get; set; }
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
        public int CommentsCount { get; set; }
        public ReactionType? CurrentUserReaction { get; set; }

        public List<CommentViewModel> Comments { get; set; } = [];
    }
}