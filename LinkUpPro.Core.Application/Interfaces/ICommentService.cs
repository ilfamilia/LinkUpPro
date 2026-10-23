using LinkUpPro.Core.Application.Dtos.Comment;
using LinkUpPro.Core.Application.Results;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface ICommentService
    {
        Task<ServiceResult> AddAsync(CreateCommentDto dto);

        Task<ServiceResult> UpdateAsync(UpdateCommentDto dto, int commentId);

        Task<ServiceResult> DeleteAsync(int commentId, string userId);

        Task<ServiceResultWithData<CommentDto>> GetMyCommentByIdAsync(int commentId, string userId);

        Task<ServiceResultWithData<List<CommentDto>>> GetCommentsByPostIdAsync(int postId, string userId);

        Task<ServiceResultWithData<Dictionary<int, List<CommentDto>>>> GetCommentsByPostIdsAsync(IEnumerable<int> postIds, string userId);
    }
}
