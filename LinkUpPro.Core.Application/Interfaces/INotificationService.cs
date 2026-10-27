using LinkUpPro.Core.Application.Dtos.Notification;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface INotificationService
    {
        Task<ServiceResult> CreateForCommentAsync(int commentId);
        Task<ServiceResult> CreateForReactionAsync(int postId, string actorUserId,
            ReactionType reactionType, bool isChange);

        Task<ServiceResultWithData<List<NotificationDto>>> GetMineAsync(string recipientUserId);
        Task<ServiceResultWithData<int>> GetUnreadCountAsync(string recipientUserId);
        Task<ServiceResult> MarkAsReadAsync(int notificationId, string recipientUserId);
        Task<ServiceResult> MarkAllAsReadAsync(string recipientUserId);
    }
}
