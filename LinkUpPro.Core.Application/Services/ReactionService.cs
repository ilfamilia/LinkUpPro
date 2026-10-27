using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Interfaces;

namespace LinkUpPro.Core.Application.Services
{
    public class ReactionService : BaseService, IReactionService
    {
        private readonly IReactionRepository _reactionRepository;
        private readonly IPostService _postService;
        private readonly INotificationService _notificationService;
        private readonly ITransactionManager _transactionManager;

        public ReactionService(IReactionRepository reactionRepository, IPostService postService,
            INotificationService notificationService, ITransactionManager transactionManager)
        {
            _reactionRepository = reactionRepository;
            _postService = postService;
            _notificationService = notificationService;
            _transactionManager = transactionManager;
        }

        public async Task<ServiceResult> ToggleReactionAsync(int postId, string userId, ReactionType reactionType)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ServiceResult.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado");
                }

                if (postId <= 0)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "El ID de la publicación es inválido");
                }

                if (reactionType is not ReactionType.Like and not ReactionType.Dislike)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "El tipo de reacción es inválido");
                }

                var postResult = await _postService.GetVisiblePostByIdAsync(postId, userId);

                if (!postResult.IsSuccess)
                {
                    return ServiceResult.Failure(postResult.Status, postResult.Errors.ToArray());
                }

                if (postResult.Data == null)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La publicación no está disponible");
                }

                Reaction? reaction = await _reactionRepository.GetByIdAsync(userId, postId);

                // Eliminar una reacción no debe generar notificación
                if (reaction != null && reaction.Type == reactionType)
                {
                    bool deleted = await _reactionRepository.DeleteAsync(userId, postId);
                    return deleted ? ServiceResult.Success()
                        : ServiceResult.Failure(OperationStatus.Conflict,
                            "No se pudo eliminar la reacción");
                }

                bool isChange = reaction != null;
                bool transactionOpen = false;

                try
                {
                    await _transactionManager.BeginTransactionAsync();
                    transactionOpen = true;

                    if (reaction == null)
                    {
                        Reaction newReaction = new()
                        {
                            UserId = userId,
                            PostId = postId,
                            Type = reactionType
                        };

                        await _reactionRepository.AddAsync(newReaction);
                    }
                    else
                    {
                        reaction.Type = reactionType;
                        Reaction? updated = await _reactionRepository.UpdateAsync(
                            reaction, userId, postId);

                        if (updated == null)
                        {
                            await _transactionManager.RollbackAsync();
                            transactionOpen = false;
                            return ServiceResult.Failure(OperationStatus.Conflict,
                                "No se pudo actualizar la reacción");
                        }
                    }

                    ServiceResult notificationResult =
                        await _notificationService.CreateForReactionAsync(
                            postId, userId, reactionType, isChange);

                    if (!notificationResult.IsSuccess)
                    {
                        await _transactionManager.RollbackAsync();
                        transactionOpen = false;
                        return ServiceResult.Failure(notificationResult.Status,
                            notificationResult.Errors.ToArray());
                    }

                    await _transactionManager.CommitAsync();
                    transactionOpen = false;
                    return ServiceResult.Success();
                }
                catch
                {
                    if (transactionOpen)
                    {
                        try
                        {
                            await _transactionManager.RollbackAsync();
                        }
                        catch
                        {
                            // No ocultar el fallo original si también falla la reversión
                        }
                    }

                    return ServiceResult.Failure(OperationStatus.Failed,
                        "No se pudo registrar la reacción y su notificación.");
                }
            });
        }
    }
}
