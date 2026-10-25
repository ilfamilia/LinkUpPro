namespace LinkUpPro.Core.Application.Dtos.Social;

/// <summary>Combines a public friend profile with its current mutual-friend count.</summary>
public sealed class FriendDto
{
    public UserSummaryDto User { get; set; } = new();
    public int MutualFriendCount { get; set; }
}
