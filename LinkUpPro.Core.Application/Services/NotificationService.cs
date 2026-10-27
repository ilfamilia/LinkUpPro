using LinkUpPro.Core.Application.Dtos.Notification;
using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Core.Application.Services
{
    public class NotificationService : BaseService, INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IPostRepository _postRepository;
        private readonly IReactionRepository _reactionRepository;
        private readonly IUserAccountStatusService _userAccountStatusService;
        private readonly IAccountServiceForWebApp _accountService;

        public NotificationService(
            INotificationRepository notificationRepository,
            ICommentRepository commentRepository,
            IPostRepository postRepository,
            IReactionRepository reactionRepository,
            IUserAccountStatusService userAccountStatusService,
            IAccountServiceForWebApp accountService)
        {
            _notificationRepository = notificationRepository;
            _commentRepository = commentRepository;
            _postRepository = postRepository;
            _reactionRepository = reactionRepository;
            _userAccountStatusService = userAccountStatusService;
            _accountService = accountService;
        }

        public async Task<ServiceResult> CreateForCommentAsync(int commentId)
        {
            return await ExecuteAsync(async () =>
            {
                if (commentId <= 0)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "El identificador del comentario es inválido.");
                }

                Comment? comment = await _commentRepository.GetByIdAsync(commentId);
                if (comment == null || comment.IsDeleted)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "El comentario no se encuentra disponible.");
                }

                Post? post = await _postRepository.GetByIdAsync(comment.PostId);
                if (post == null || post.IsDeleted)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La publicación no se encuentra disponible.");
                }

                if (!await _userAccountStatusService.IsUserActiveAsync(comment.UserId))
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "La cuenta que realizó el comentario no está activa.");
                }

                bool isReply = comment.ParentCommentId.HasValue;
                string recipientUserId;
                NotificationType type;

                if (isReply)
                {
                    Comment? parent = await _commentRepository.GetByIdAsync(
                        comment.ParentCommentId!.Value);

                    if (parent == null || parent.IsDeleted || parent.PostId != comment.PostId)
                    {
                        return ServiceResult.Failure(OperationStatus.NotFound,
                            "El comentario respondido no se encuentra disponible.");
                    }

                    recipientUserId = parent.UserId;
                    type = NotificationType.Reply;
                }
                else
                {
                    recipientUserId = post.UserId;
                    type = NotificationType.Comment;
                }

                // No se crean notificaciones por interacciones propias
                if (comment.UserId == recipientUserId)
                {
                    return ServiceResult.Success();
                }

                UserDto? actor = await _accountService.GetUserByIdAsync(comment.UserId);
                if (actor == null)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "No fue posible obtener el usuario que realizó el comentario.");
                }

                string description = type == NotificationType.Reply
                    ? $"{actor.UserName} respondió tu comentario."
                    : $"{actor.UserName} comentó tu publicación.";

                Notification notification = new()
                {
                    Id = 0,
                    ActorUserId = comment.UserId,
                    RecipientUserId = recipientUserId,
                    Type = type,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    PostId = comment.PostId,
                    CommentId = comment.Id
                };

                await _notificationRepository.AddAsync(notification);
                return ServiceResult.Success();
            });
        }

        public async Task<ServiceResult> CreateForReactionAsync(int postId,
            string actorUserId, ReactionType reactionType, bool isChange)
        {
            return await ExecuteAsync(async () =>
            {
                if (postId <= 0 || string.IsNullOrWhiteSpace(actorUserId) ||
                    reactionType is not ReactionType.Like and not ReactionType.Dislike)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "Los datos de la reacción son inválidos.");
                }

                Post? post = await _postRepository.GetByIdAsync(postId);
                if (post == null || post.IsDeleted)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La publicación no se encuentra disponible.");
                }

                if (post.UserId == actorUserId)
                {
                    return ServiceResult.Success();
                }

                if (!await _userAccountStatusService.IsUserActiveAsync(actorUserId))
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "La cuenta que realizó la reacción no está activa.");
                }

                Reaction? reaction = await _reactionRepository.GetByIdAsync(actorUserId, postId);
                if (reaction == null || reaction.Type != reactionType)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "La reacción no se encuentra registrada.");
                }

                UserDto? actor = await _accountService.GetUserByIdAsync(actorUserId);
                if (actor == null)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "No fue posible obtener el usuario que realizó la reacción.");
                }

                string action = reactionType == ReactionType.Like ? "Me gusta" : "No me gusta";
                string description = isChange
                    ? $"{actor.UserName} cambió su reacción a {action} en tu publicación."
                    : $"{actor.UserName} reaccionó con {action} a tu publicación.";

                Notification notification = new()
                {
                    Id = 0,
                    ActorUserId = actorUserId,
                    RecipientUserId = post.UserId,
                    Type = NotificationType.Reaction,
                    Description = description,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    PostId = postId,
                    CommentId = null
                };

                await _notificationRepository.AddAsync(notification);
                return ServiceResult.Success();
            });
        }

        public async Task<ServiceResultWithData<List<NotificationDto>>> GetMineAsync(
            string recipientUserId)
        {
            return await ExecuteAsync<List<NotificationDto>>(async () =>
            {
                ServiceResult? accessError = await ValidateRecipientAsync(recipientUserId);
                if (accessError != null)
                {
                    return ServiceResultWithData<List<NotificationDto>>.Failure(
                        accessError.Status, accessError.Errors.ToArray());
                }

                List<Notification> notifications = await _notificationRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(n => n.RecipientUserId == recipientUserId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ThenByDescending(n => n.Id)
                    .ToListAsync();

                Dictionary<string, UserDto?> actors = new();
                List<NotificationDto> result = new(notifications.Count);

                foreach (Notification notification in notifications)
                {
                    if (!actors.TryGetValue(notification.ActorUserId, out UserDto? actor))
                    {
                        actor = await _accountService.GetUserByIdAsync(notification.ActorUserId);
                        actors[notification.ActorUserId] = actor;
                    }

                    result.Add(new NotificationDto
                    {
                        Id = notification.Id,
                        ActorUserId = notification.ActorUserId,
                        ActorUserName = actor?.UserName ?? "Usuario no disponible",
                        ActorProfileImagePath = actor?.ProfileImage,
                        Type = notification.Type,
                        Description = notification.Description,
                        CreatedAt = notification.CreatedAt,
                        IsRead = notification.IsRead,
                        PostId = notification.PostId,
                        CommentId = notification.CommentId
                    });
                }

                return ServiceResultWithData<List<NotificationDto>>.Success(result);
            });
        }

        public async Task<ServiceResultWithData<int>> GetUnreadCountAsync(
            string recipientUserId)
        {
            return await ExecuteAsync<int>(async () =>
            {
                ServiceResult? accessError = await ValidateRecipientAsync(recipientUserId);
                if (accessError != null)
                {
                    return ServiceResultWithData<int>.Failure(
                        accessError.Status, accessError.Errors.ToArray());
                }

                int count = await _notificationRepository.GetAllQuery()
                    .CountAsync(n => n.RecipientUserId == recipientUserId && !n.IsRead);

                return ServiceResultWithData<int>.Success(count);
            });
        }

        public async Task<ServiceResult> MarkAsReadAsync(int notificationId,
            string recipientUserId)
        {
            return await ExecuteAsync(async () =>
            {
                ServiceResult? accessError = await ValidateRecipientAsync(recipientUserId);
                if (accessError != null)
                {
                    return accessError;
                }

                if (notificationId <= 0)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "El identificador de la notificación es inválido.");
                }

                Notification? notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification == null)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La notificación no existe.");
                }

                if (notification.RecipientUserId != recipientUserId)
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No posee permisos para modificar esta notificación.");
                }

                if (notification.IsRead)
                {
                    return ServiceResult.Success();
                }

                notification.IsRead = true;
                Notification? updated = await _notificationRepository.UpdateAsync(
                    notification, notification.Id);

                return updated == null
                    ? ServiceResult.Failure(OperationStatus.NotFound, "La notificación no existe.")
                    : ServiceResult.Success();
            });
        }

        public async Task<ServiceResult> MarkAllAsReadAsync(string recipientUserId)
        {
            return await ExecuteAsync(async () =>
            {
                ServiceResult? accessError = await ValidateRecipientAsync(recipientUserId);
                if (accessError != null)
                {
                    return accessError;
                }

                await _notificationRepository.MarkAllAsReadAsync(recipientUserId);
                return ServiceResult.Success();
            });
        }

        private async Task<ServiceResult?> ValidateRecipientAsync(string recipientUserId)
        {
            if (string.IsNullOrWhiteSpace(recipientUserId))
            {
                return ServiceResult.Failure(OperationStatus.Unauthorized,
                    "El usuario no está autenticado.");
            }

            if (!await _userAccountStatusService.IsUserActiveAsync(recipientUserId))
            {
                return ServiceResult.Failure(OperationStatus.Forbidden,
                    "La cuenta del usuario no está activa.");
            }

            return null;
        }
    }
}
