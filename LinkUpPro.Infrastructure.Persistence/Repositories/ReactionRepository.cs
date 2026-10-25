using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Interfaces;
using LinkUpPro.Core.Domain.Queries;
using LinkUpPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Infrastructure.Persistence.Repositories
{
    public class ReactionRepository : GenericRepository<Reaction>, IReactionRepository
    {
        public ReactionRepository(LinkUpProAppContext context) : base(context)
        {
        }

        public async Task<Dictionary<int, PostReactionCount>> CountByPostIdsAsync(IReadOnlyCollection<int> postIds)
        {
            return await _dbSet
                .Where(r => postIds.Contains(r.PostId))
                .GroupBy(r => r.PostId)
                .Select(g => new PostReactionCount
                {
                    PostId = g.Key,
                    LikesCount = g.Count(r => r.Type == ReactionType.Like),
                    DislikesCount = g.Count(r => r.Type == ReactionType.Dislike)
                })
                .ToDictionaryAsync(r => r.PostId);
        }

        public async Task<Dictionary<int, ReactionType>> GetUserReactionsAsync(string userId,
            IReadOnlyCollection<int> postIds)
        {
            return await _dbSet
                .Where(r => r.UserId == userId && postIds.Contains(r.PostId))
                .ToDictionaryAsync(r => r.PostId, r => r.Type);
        }
    }
}
