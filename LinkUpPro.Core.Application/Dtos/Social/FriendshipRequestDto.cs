using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Social;

/// <summary>Displays a request without allowing the browser to choose its sender or state.</summary>
public sealed class FriendshipRequestDto
{
    public int Id { get; set; }
    public UserSummaryDto OtherUser { get; set; } = new();
    public int MutualFriendCount { get; set; }
    public FriendshipRequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
