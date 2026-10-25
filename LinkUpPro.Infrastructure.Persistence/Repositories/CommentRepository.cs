using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Interfaces;
using LinkUpPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Infrastructure.Persistence.Repositories
{
    public class CommentRepository : GenericRepository<Comment>, ICommentRepository
    {
        public CommentRepository(LinkUpProAppContext context) : base(context)
        {
        }

        public async Task<List<Comment>> GetByPostIdsAsync(IReadOnlyCollection<int> postIds)
        {
            return await _dbSet.AsNoTracking()
                .Where(c => postIds.Contains(c.PostId))
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> CountActiveByPostIdsAsync(IReadOnlyCollection<int> postIds)
        {
            return await _dbSet
                .Where(c => postIds.Contains(c.PostId) && !c.IsDeleted)
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(c => c.PostId, c => c.Count);
        }
    }
}
