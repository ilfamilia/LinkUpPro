using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Notification
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public required string ActorUserId { get; set; }
        public required string ActorUserName { get; set; }
        public string? ActorProfileImagePath { get; set; }
        public NotificationType Type { get; set; }
        public required string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int PostId { get; set; }
        public int? CommentId { get; set; }
    }
}
