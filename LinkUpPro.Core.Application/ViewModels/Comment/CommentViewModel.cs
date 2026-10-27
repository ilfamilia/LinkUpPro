namespace LinkUpPro.Core.Application.ViewModels.Comment
{
    public class CommentViewModel
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? UserProfilePicturePath { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public int PostId { get; set; }

        public int? ParentCommentId { get; set; }

        public List<CommentViewModel> Replies { get; set; } = [];
    }
}