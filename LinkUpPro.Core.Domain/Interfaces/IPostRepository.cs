using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Queries;

namespace LinkUpPro.Core.Domain.Interfaces
{
    public interface IPostRepository : IGenericRepository<Post>
    {
        /// <summary>Returns a post that has not been logically deleted.</summary>
        Task<Post?> GetActiveByIdAsync(int postId);

        /// <summary>Returns the post only when it is the viewer's own or a "Solo amigos" post of an active friendship.</summary>
        Task<Post?> GetVisibleByIdAsync(int postId, string viewerId);

        /// <summary>Returns the posts among the ids that the viewer may see, newest first.</summary>
        Task<List<Post>> GetVisibleByIdsAsync(IReadOnlyCollection<int> postIds, string viewerId);

        /// <summary>Returns the author's own non-deleted posts with the optional filters, newest first.</summary>
        Task<List<Post>> GetByAuthorAsync(string userId, PostSearchCriteria? criteria = null);

        /// <summary>Returns the "Solo amigos" posts of the viewer's active friends, newest first.</summary>
        Task<List<Post>> GetFriendsPostsAsync(string viewerId);
    }
}
