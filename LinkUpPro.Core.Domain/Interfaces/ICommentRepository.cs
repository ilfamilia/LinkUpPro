using LinkUpPro.Core.Domain.Entities;

namespace LinkUpPro.Core.Domain.Interfaces
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        /// <summary>Returns every comment and reply of the posts, oldest first, including deleted ones to keep threads.</summary>
        Task<List<Comment>> GetByPostIdsAsync(IReadOnlyCollection<int> postIds);

        /// <summary>Counts the non-deleted comments of each post.</summary>
        Task<Dictionary<int, int>> CountActiveByPostIdsAsync(IReadOnlyCollection<int> postIds);
    }
}
