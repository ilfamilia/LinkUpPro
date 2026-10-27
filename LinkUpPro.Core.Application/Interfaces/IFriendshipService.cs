using LinkUpPro.Core.Application.Dtos.Friendship;
using LinkUpPro.Core.Application.Results;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IFriendshipService
    {
        Task<ServiceResultWithData<List<FriendDto>>> GetFriendsAsync(string currentUserId, string? searchText = null);

        Task<ServiceResultWithData<FriendDto>> GetFriendAsync(string currentUserId, string friendUserId);

        Task<ServiceResultWithData<List<FriendDto>>> GetMutualFriendsAsync(string currentUserId, string friendUserId);

        Task<ServiceResultWithData<FriendshipSummaryDto>> GetSummaryAsync(string currentUserId);

        Task<ServiceResult> RemoveAsync(string currentUserId, string friendUserId);
    }
}
