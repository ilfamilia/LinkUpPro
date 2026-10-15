using LinkUpPro.Core.Domain.Common;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Domain.Entities
{
    public class Notification : BasicEntity<int>
    {
        public required string ActorUserId { get; set; }

        public required string RecipientUserId { get; set; }

        public required NotificationType Type { get; set; }

        public required string Description { get; set; }

        public required DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; }

        public required int PostId { get; set; }

        public Post? Post { get; set; }

        public int? CommentId { get; set; }

        public Comment? Comment { get; set; }
    }
}
