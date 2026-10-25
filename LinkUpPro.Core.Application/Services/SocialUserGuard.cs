using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Dtos.Social;
using LinkUpPro.Core.Domain.Exceptions;

namespace LinkUpPro.Core.Application.Services;

/// <summary>Validates account activation independently from the temporary login lockout state.</summary>
public sealed class SocialUserGuard(ICurrentUserService currentUser, IUserDirectoryService users)
{
    /// <summary>Resolves the authenticated account and rejects inactive or removed users.</summary>
    public Task<UserSummaryDto> GetCurrentAsync() => GetActiveAsync(currentUser.GetRequiredUserId());

    /// <summary>Reads an active public profile without disclosing private Identity data.</summary>
    public async Task<UserSummaryDto> GetActiveAsync(string id)
    {
        var user = await users.GetByIdAsync(id);
        if (user is null || !user.IsActive)
            throw new DomainRuleException("La cuenta no existe o se encuentra inactiva.");
        return user;
    }
}
