namespace LinkUpPro.Core.Application.ViewModels.Comment
{
    public class CommentViewModel
    {
        public int Id { get; set; }

        // User information
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? UserProfilePicturePath { get; set; }

        // Comment information
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public int PostId { get; set; }

        public int? ParentCommentId { get; set; }

        // Replies
        public List<CommentViewModel> Replies { get; set; } = [];
    }
}