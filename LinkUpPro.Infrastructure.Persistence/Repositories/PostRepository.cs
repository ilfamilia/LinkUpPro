using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Interfaces;
using LinkUpPro.Core.Domain.Queries;
using LinkUpPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Infrastructure.Persistence.Repositories
{
    public class PostRepository : GenericRepository<Post>, IPostRepository
    {
        public PostRepository(LinkUpProAppContext context) : base(context)
        {
        }

        public async Task<Post?> GetActiveByIdAsync(int postId)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
        }

        public async Task<Post?> GetVisibleByIdAsync(int postId, string viewerId)
        {
            return await VisibleTo(viewerId).FirstOrDefaultAsync(p => p.Id == postId);
        }

        public async Task<List<Post>> GetVisibleByIdsAsync(IReadOnlyCollection<int> postIds, string viewerId)
        {
            return await VisibleTo(viewerId)
                .Where(p => postIds.Contains(p.Id))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetByAuthorAsync(string userId, PostSearchCriteria? criteria = null)
        {
            IQueryable<Post> query = _dbSet.AsNoTracking()
                .Where(p => p.UserId == userId && !p.IsDeleted);

            if (criteria != null)
            {
                if (!string.IsNullOrWhiteSpace(criteria.SearchText))
                {
                    string searchText = criteria.SearchText.Trim();

                    query = query.Where(p => p.Content.Contains(searchText));
                }

                if (criteria.MediaType.HasValue)
                {
                    query = query.Where(p => p.MediaType == criteria.MediaType.Value);
                }

                if (criteria.CreatedFrom.HasValue)
                {
                    query = query.Where(p => p.CreatedAt >= criteria.CreatedFrom.Value);
                }

                if (criteria.CreatedTo.HasValue)
                {
                    query = query.Where(p => p.CreatedAt <= criteria.CreatedTo.Value);
                }

                if (criteria.IsEdited.HasValue)
                {
                    bool isEdited = criteria.IsEdited.Value;

                    query = query.Where(p => (p.UpdatedAt != null) == isEdited);
                }
            }

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<List<Post>> GetFriendsPostsAsync(string viewerId)
        {
            IQueryable<Friendship> friendships = ActiveFriendshipsOf(viewerId);

            return await _dbSet.AsNoTracking()
                .Where(p => !p.IsDeleted && p.UserId != viewerId &&
                    p.Privacy == PostPrivacy.FriendsOnly &&
                    friendships.Any(f =>
                        (f.User1Id == viewerId && f.User2Id == p.UserId) ||
                        (f.User2Id == viewerId && f.User1Id == p.UserId)))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        // Own posts, or "Solo amigos" posts whose author still has an active friendship with the viewer.
        private IQueryable<Post> VisibleTo(string viewerId)
        {
            IQueryable<Friendship> friendships = ActiveFriendshipsOf(viewerId);

            return _dbSet.AsNoTracking()
                .Where(p => !p.IsDeleted &&
                    (
                        p.UserId == viewerId ||
                        (
                            p.Privacy == PostPrivacy.FriendsOnly &&
                            friendships.Any(f =>
                                (f.User1Id == viewerId && f.User2Id == p.UserId) ||
                                (f.User2Id == viewerId && f.User1Id == p.UserId))
                        )
                    ));
        }

        private IQueryable<Friendship> ActiveFriendshipsOf(string userId)
        {
            return _dbContext.Set<Friendship>()
                .Where(f => f.Status == FriendshipStatus.Active &&
                    (f.User1Id == userId || f.User2Id == userId));
        }
    }
}
