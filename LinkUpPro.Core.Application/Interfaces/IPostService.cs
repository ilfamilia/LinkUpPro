using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Application.Results;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IPostService
    {
        Task<ServiceResult> AddAsync(CreatePostDto dto);

        Task<ServiceResult> UpdateAsync(UpdatePostDto dto, int postId);

        Task<ServiceResult> DeleteAsync(int postId, string userId);

        Task<ServiceResultWithData<List<PostDto>>> GetMyPostsAsync(string userId, PostFilterDto? filter = null);

        Task<ServiceResultWithData<List<PostDto>>> GetFriendsPostsAsync(string userId);

        Task<ServiceResultWithData<PostDto>> GetMyPostByIdAsync(int postId, string userId);

        Task<ServiceResultWithData<PostDto>> GetVisiblePostByIdAsync(int postId, string userId);

        Task<ServiceResultWithData<HashSet<int>>> GetVisiblePostIdsAsync(IEnumerable<int> postIds, string userId);
    }
}
