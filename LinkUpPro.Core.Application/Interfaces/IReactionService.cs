using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IReactionService
    {
        Task<ServiceResult> ToggleReactionAsync(int postId, string userId, ReactionType reactionType);
    }
}
