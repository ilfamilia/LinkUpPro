using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Social;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.Core.Domain.Interfaces;

namespace LinkUpPro.Core.Application.Services;

/// <summary>Creates interaction notifications from persisted facts and protects recipient-only access.</summary>
public sealed class NotificationService(
    INotificationRepository notifications, IPostRepository posts, ICommentRepository comments,
    IReactionRepository reactions, IFriendshipService friends, IUserDirectoryService users,
    SocialUserGuard guard, IAtomicOperation atomic, TimeProvider clock, IMapper mapper) : INotificationService
{
    /// <summary>Lists the caller's notifications newest first with current public actor data.</summary>
    public async Task<List<NotificationDto>> GetAllAsync()
    {
        var current = await guard.GetCurrentAsync();
        var list = await notifications.GetListAsync(n => n.RecipientUserId == current.Id);
        var actors = (await users.GetByIdsAsync(list.Select(n => n.ActorUserId))).ToDictionary(u => u.Id);
        return list.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).Select(n =>
        {
            var dto = mapper.Map<NotificationDto>(n);
            dto.Actor = actors.GetValueOrDefault(n.ActorUserId) ?? new() { UserName = "Usuario no disponible" };
            return dto;
        }).ToList();
    }

    /// <summary>Counts unread notifications without revealing another recipient's data.</summary>
    public async Task<int> GetUnreadCountAsync()
    {
        var current = await guard.GetCurrentAsync();
        return (await notifications.GetListAsync(n => n.RecipientUserId == current.Id && !n.IsRead)).Count;
    }

    /// <summary>Marks a single owned notification as read.</summary>
    public Task MarkReadAsync(int notificationId) => atomic.ExecuteAsync(async () =>
    {
        (await GetOwnedAsync(notificationId)).IsRead = true;
        return true;
    });

    /// <summary>Marks all unread notifications for the current recipient only.</summary>
    public Task MarkAllReadAsync() => atomic.ExecuteAsync(async () =>
    {
        var current = await guard.GetCurrentAsync();
        foreach (var notification in await notifications.GetListAsync(n => n.RecipientUserId == current.Id && !n.IsRead))
            notification.IsRead = true;
        return true;
    });

    /// <summary>Revalidates target existence and current privacy before returning an internal content identifier.</summary>
    public async Task<(int PostId, int? CommentId)> GetContentAsync(int notificationId)
    {
        var notification = await GetOwnedAsync(notificationId);
        var current = await guard.GetCurrentAsync();
        await GetVisiblePostAsync(notification.PostId, current.Id);
        if (notification.CommentId.HasValue)
        {
            var comment = await comments.GetByIdAsync(notification.CommentId.Value);
            if (comment is null || comment.IsDeleted || comment.PostId != notification.PostId)
                throw new DomainRuleException("El contenido relacionado con esta notificación ya no se encuentra disponible.");
        }
        return (notification.PostId, notification.CommentId);
    }

    /// <summary>Notifies the post author or the directly replied-to author after a persisted comment.</summary>
    public Task NotifyCommentAsync(int commentId) => atomic.ExecuteAsync(async () =>
    {
        var current = await guard.GetCurrentAsync();
        var comment = await comments.GetByIdAsync(commentId);
        if (comment is null || comment.IsDeleted || comment.UserId != current.Id)
            throw new DomainRuleException("No posee permisos para notificar este comentario.");
        var post = await GetVisiblePostAsync(comment.PostId, current.Id);
        var recipient = post.UserId;
        var type = NotificationType.Comment;
        if (comment.ParentCommentId.HasValue)
        {
            var parent = await comments.GetByIdAsync(comment.ParentCommentId.Value);
            if (parent is null || parent.PostId != post.Id)
                throw new DomainRuleException("La respuesta no pertenece a esta publicación.");
            recipient = parent.UserId;
            type = NotificationType.Reply;
        }
        var description = type == NotificationType.Reply
            ? $"{current.UserName} respondió tu comentario."
            : $"{current.UserName} comentó tu publicación.";
        await CreateAsync(current.Id, recipient, post.Id, comment.Id, type, description);
        return true;
    });

    /// <summary>Notifies the post author of a persisted reaction registration or change, never removal.</summary>
    public Task NotifyReactionAsync(int postId) => atomic.ExecuteAsync(async () =>
    {
        var current = await guard.GetCurrentAsync();
        var post = await GetVisiblePostAsync(postId, current.Id);
        var reaction = await reactions.GetByIdAsync(current.Id, postId);
        if (reaction is null)
            throw new DomainRuleException("No existe una reacción para notificar.");
        var label = reaction.Type == ReactionType.Like ? "Me gusta" : "No me gusta";
        await CreateAsync(current.Id, post.UserId, postId, null, NotificationType.Reaction,
            $"{current.UserName} reaccionó con {label} a tu publicación.");
        return true;
    });

    /// <summary>Obtains a notification only after validating its recipient.</summary>
    private async Task<Notification> GetOwnedAsync(int id)
    {
        var current = await guard.GetCurrentAsync();
        var notification = await notifications.GetByIdAsync(id);
        if (notification is null || notification.RecipientUserId != current.Id)
            throw new DomainRuleException("No posee permisos para consultar esta notificación.");
        return notification;
    }

    /// <summary>Shares the current friendship and privacy checks needed when following notification links.</summary>
    private async Task<Post> GetVisiblePostAsync(int id, string userId)
    {
        var post = await posts.GetByIdAsync(id);
        var author = post is null ? null : await users.GetByIdAsync(post.UserId);
        if (post is null || post.IsDeleted || author is null || !author.IsActive
            || (post.UserId != userId && (post.Privacy != PostPrivacy.FriendsOnly
                || !await friends.AreFriendsAsync(userId, post.UserId))))
            throw new DomainRuleException("El contenido relacionado con esta notificación ya no se encuentra disponible para usted.");
        return post;
    }

    /// <summary>Skips self-notifications and missing recipients while preserving history for inactive recipients.</summary>
    private async Task CreateAsync(string actor, string recipient, int postId, int? commentId, NotificationType type, string description)
    {
        if (actor == recipient) return;
        var target = await users.GetByIdAsync(recipient);
        if (target is null) return;
        await notifications.AddAsync(new() { Id = 0, ActorUserId = actor, RecipientUserId = recipient,
            PostId = postId, CommentId = commentId, Type = type, Description = description,
            CreatedAt = clock.GetUtcNow().UtcDateTime, IsRead = false });
    }
}
