using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Queries;

namespace LinkUpPro.Core.Domain.Interfaces
{
    public interface IReactionRepository : IGenericRepository<Reaction>
    {
        /// <summary>Counts likes and dislikes of each post.</summary>
        Task<Dictionary<int, PostReactionCount>> CountByPostIdsAsync(IReadOnlyCollection<int> postIds);

        /// <summary>Returns the reaction the user left on each of the posts.</summary>
        Task<Dictionary<int, ReactionType>> GetUserReactionsAsync(string userId, IReadOnlyCollection<int> postIds);
    }
}
