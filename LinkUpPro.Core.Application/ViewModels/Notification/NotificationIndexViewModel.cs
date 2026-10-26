namespace LinkUpPro.Core.Application.ViewModels.Notification
{
    public class NotificationIndexViewModel
    {
        public List<NotificationViewModel> Notifications { get; set; } = [];
        public int UnreadCount { get; set; }
    }
}