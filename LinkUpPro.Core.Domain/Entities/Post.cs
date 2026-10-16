using LinkUpPro.Core.Domain.Common;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Domain.Entities
{
    public class Post : BasicEntity<int>
    {
        public required string UserId { get; set; }

        public required string Content { get; set; }

        public required PostPrivacy Privacy { get; set; }

        public required PostMediaType MediaType { get; set; }

        public string? ImagePath { get; set; }

        public string? YouTubeUrl { get; set; }

        public bool AllowComments { get; set; }

        public required DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
        
        public bool IsDeleted { get; set; }

        public ICollection<Comment>? Comments { get; set; }

        public ICollection<Notification>? Notifications { get; set; }

        public ICollection<Reaction>? Reactions { get; set; }
    }
}
