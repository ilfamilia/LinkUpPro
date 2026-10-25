using LinkUpPro.Core.Application.Dtos.Social;

namespace LinkUpPro.Core.Application.Interfaces;

/// <summary>Reads public Identity profiles while keeping Identity types outside Application.</summary>
public interface IUserDirectoryService
{
    /// <summary>Reads one public profile and its current activation state.</summary>
    Task<UserSummaryDto?> GetByIdAsync(string userId);
    /// <summary>Reads public profiles in a batch without private account fields.</summary>
    Task<List<UserSummaryDto>> GetByIdsAsync(IEnumerable<string> userIds);
    /// <summary>Searches active accounts by a partial normalized username.</summary>
    Task<List<UserSummaryDto>> SearchActiveAsync(string? search);
}
