using LinkUpPro.Core.Application.Dtos.Social;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Infrastructure.Identity.Contexts;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Infrastructure.Identity.Services;

/// <summary>Adapts Identity accounts to the public profile contract used by social modules.</summary>
public sealed class UserDirectoryService(IdentityContext context) : IUserDirectoryService
{
    /// <summary>Reads an account's public profile; email confirmation represents activation.</summary>
    public async Task<UserSummaryDto?> GetByIdAsync(string userId)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);
        return user is null ? null : Project(user);
    }

    /// <summary>Reads multiple public profiles in one database query.</summary>
    public async Task<List<UserSummaryDto>> GetByIdsAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        return (await context.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToListAsync()).Select(Project).ToList();
    }

    /// <summary>Searches confirmed accounts by normalized partial username.</summary>
    public async Task<List<UserSummaryDto>> SearchActiveAsync(string? search)
    {
        var term = search?.Trim().ToUpperInvariant() ?? "";
        return (await context.Users.AsNoTracking().Where(u => u.EmailConfirmed
            && (term == "" || u.NormalizedUserName != null && u.NormalizedUserName.Contains(term)))
            .OrderBy(u => u.UserName).ToListAsync()).Select(Project).ToList();
    }

    /// <summary>Excludes private contact, password, token, and role data from module results.</summary>
    private static UserSummaryDto Project(AppUser user) => new()
    {
        Id = user.Id, UserName = user.UserName ?? "", FirstName = user.FirstName, LastName = user.LastName,
        ProfilePicturePath = user.ProfilePicturePath, IsActive = user.EmailConfirmed
    };
}
