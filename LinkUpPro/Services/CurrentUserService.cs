using System.Security.Claims;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Exceptions;

namespace LinkUpPro.Services;

/// <summary>Resolves the authenticated Identity claim at the Web boundary.</summary>
public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    /// <summary>Rejects unauthenticated callers rather than trusting a posted user identifier.</summary>
    public string GetRequiredUserId()
    {
        var principal = accessor.HttpContext?.User;
        var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (principal?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(id))
            throw new DomainRuleException("Debe iniciar sesión para acceder a esta sección.");
        return id;
    }
}
