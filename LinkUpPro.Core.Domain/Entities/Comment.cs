using LinkUpPro.Core.Domain.Common;

namespace LinkUpPro.Core.Domain.Entities
{
    public class Comment : BasicEntity<int>
    {
        public required string UserId { get; set; }

        public required string Content { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public required int PostId { get; set; }

        public Post? Post { get; set; }

        public int? ParentCommentId { get; set; }

        public Comment? ParentComment { get; set; }

        public ICollection<Comment>? Replies { get; set; }
    }
}
