using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Application.Dtos.FriendshipRequest;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IFriendshipRequestService
    {
        Task<ServiceResult> SendAsync(string senderUserId, string receiverUserId);

        Task<ServiceResult> AcceptAsync(int requestId, string currentUserId);

        Task<ServiceResult> RejectAsync(int requestId, string currentUserId);

        Task<ServiceResult> CancelAsync(int requestId, string currentUserId);

        Task<ServiceResult> HideFromHistoryAsync(int requestId, string currentUserId);

        Task<ServiceResultWithData<List<FriendshipRequestListDto>>> GetReceivedPendingAsync(string currentUserId);

        Task<ServiceResultWithData<List<FriendshipRequestListDto>>> GetSentAsync(string currentUserId);

        Task<ServiceResultWithData<List<AvailableFriendshipUserDto>>> GetAvailableUsersAsync(string currentUserId, string? userNameFilter = null);

        Task<ServiceResultWithData<List<FriendshipUserDto>>> GetMutualFriendsAsync(string currentUserId, string otherUserId);

        Task<ServiceResultWithData<int>> GetReceivedPendingCountAsync(string currentUserId);
    }
}