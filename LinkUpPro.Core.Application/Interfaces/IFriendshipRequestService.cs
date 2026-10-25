using LinkUpPro.Core.Application.Dtos.Social;

namespace LinkUpPro.Core.Application.Interfaces;

/// <summary>Controls request eligibility, recipient responses, sender cancellation, and sender history.</summary>
public interface IFriendshipRequestService
{
    /// <summary>Lists actionable received pending requests.</summary>
    Task<List<FriendshipRequestDto>> GetIncomingAsync();
    /// <summary>Lists visible sent history without cancelled or hidden entries.</summary>
    Task<List<FriendshipRequestDto>> GetSentAsync();
    /// <summary>Finds active users without a friendship or pending request in either direction.</summary>
    Task<List<FriendDto>> GetEligibleUsersAsync(string? search = null);
    /// <summary>Counts actionable received requests for the navigation badge.</summary>
    Task<int> GetPendingCountAsync();
    /// <summary>Creates a request using the trusted authenticated sender.</summary>
    Task SendAsync(string receiverId);
    /// <summary>Accepts a received request and creates or reactivates friendship atomically.</summary>
    Task AcceptAsync(int requestId);
    /// <summary>Rejects a received request while preserving its history.</summary>
    Task RejectAsync(int requestId);
    /// <summary>Cancels a still-pending request owned by the sender.</summary>
    Task CancelAsync(int requestId);
    /// <summary>Hides accepted or rejected sender history without changing the relationship.</summary>
    Task HideAsync(int requestId);
}
