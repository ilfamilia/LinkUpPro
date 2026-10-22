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

        public ReactionService(IReactionRepository reactionRepository, IPostService postService)
        {
            _reactionRepository = reactionRepository;
            _postService = postService;
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

                if (reaction == null)
                {
                    Reaction newReaction = new()
                    {
                        UserId = userId,
                        PostId = postId,
                        Type = reactionType
                    };

                    await _reactionRepository.AddAsync(newReaction);

                    return ServiceResult.Success();
                }

                if (reaction.Type == reactionType)
                {
                    bool deleted = await _reactionRepository.DeleteAsync(userId, postId);

                    return deleted ? ServiceResult.Success() : ServiceResult.Failure(OperationStatus.Conflict,
                            "No se pudo eliminar la reacción");
                }

                reaction.Type = reactionType;

                Reaction? updatedReaction = await _reactionRepository.UpdateAsync(reaction, userId, postId);

                return updatedReaction != null ? ServiceResult.Success() : ServiceResult.Failure(OperationStatus.Conflict,
                        "No se pudo actualizar la reacción");
            });
        }
    }
}
