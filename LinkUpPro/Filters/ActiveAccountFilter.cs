using LinkUpPro.Core.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LinkUpPro.Filters;

/// <summary>Checks account activation on every protected module action, including invalid form submissions.</summary>
public sealed class ActiveAccountFilter(SocialUserGuard guard) : IAsyncActionFilter
{
    /// <summary>Prevents an authenticated but inactive account from reaching a module action.</summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Public actions (for example the global error page) are reachable without a session.
        if (!context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            await guard.GetCurrentAsync();
        }

        await next();
    }
}
