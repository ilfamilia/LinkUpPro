using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.ViewModels.Notification
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public string ActorUserName { get; set; } = string.Empty;
        public string? ActorProfileImagePath { get; set; }
        public NotificationType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int PostId { get; set; }
        public int? CommentId { get; set; }
    }
}
