using LinkUpPro.Core.Application.Dtos.Social;

namespace LinkUpPro.Core.Application.Interfaces;

/// <summary>Persists interaction notifications and limits reading or updating to their recipient.</summary>
public interface INotificationService
{
    /// <summary>Lists notifications belonging only to the current recipient.</summary>
    Task<List<NotificationDto>> GetAllAsync();
    /// <summary>Counts unread notifications for the current recipient.</summary>
    Task<int> GetUnreadCountAsync();
    /// <summary>Marks one owned notification as read.</summary>
    Task MarkReadAsync(int notificationId);
    /// <summary>Marks all owned unread notifications as read.</summary>
    Task MarkAllReadAsync();
    /// <summary>Rechecks target existence and permission before returning navigation identifiers.</summary>
    Task<(int PostId, int? CommentId)> GetContentAsync(int notificationId);
    // Call these operations from the interaction services after the interaction has been persisted.
    /// <summary>Notifies the correct author of a persisted comment or reply.</summary>
    Task NotifyCommentAsync(int commentId);
    /// <summary>Notifies the post author after a persisted reaction registration or change.</summary>
    Task NotifyReactionAsync(int postId);
}
