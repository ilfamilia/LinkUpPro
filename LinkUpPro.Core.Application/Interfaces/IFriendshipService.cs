using LinkUpPro.Core.Application.Dtos.Social;

namespace LinkUpPro.Core.Application.Interfaces;

/// <summary>Manages current friendships and exposes authorization checks to other modules.</summary>
public interface IFriendshipService
{
    /// <summary>Checks the current relationship and activation of both accounts.</summary>
    Task<bool> AreFriendsAsync(string firstUserId, string secondUserId);
    /// <summary>Returns active friend identifiers for internal authorization checks.</summary>
    Task<List<string>> GetActiveFriendIdsAsync(string userId);
    /// <summary>Lists and searches the caller's friends and mutual-friend counts.</summary>
    Task<List<FriendDto>> GetFriendsAsync(string? search = null);
    /// <summary>Reads a profile only while an active friendship grants access.</summary>
    Task<UserSummaryDto> GetFriendProfileAsync(string userId);
    /// <summary>Lists active friends shared by the caller and the selected user.</summary>
    Task<List<UserSummaryDto>> GetMutualFriendsAsync(string otherUserId);
    /// <summary>Logically removes a friendship without deleting historical records.</summary>
    Task RemoveAsync(string otherUserId);
    /// <summary>Filters publications only after applying friendship and privacy rules.</summary>
    Task<List<FriendPostDto>> GetFriendPostsAsync(FriendPostFilterDto filter);
}
