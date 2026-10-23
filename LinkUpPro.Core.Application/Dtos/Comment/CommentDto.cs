using LinkUpPro.Core.Application.Dtos.Common;

namespace LinkUpPro.Core.Application.Dtos.Comment
{
    public class CommentDto : BasicDto<int>
    {
        public required string UserId { get; set; }

        public required string Content { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public int PostId { get; set; }

        public int? ParentCommentId { get; set; }

        public List<CommentDto> Replies { get; set; } = [];
    }
}