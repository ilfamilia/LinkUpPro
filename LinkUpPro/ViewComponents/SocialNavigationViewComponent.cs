using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.ViewModels.Social;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.ViewComponents;

/// <summary>Refreshes social menu counters only when an authenticated page is rendered.</summary>
public sealed class SocialNavigationViewComponent(IFriendshipRequestService requests, INotificationService notifications,
    ILogger<SocialNavigationViewComponent> logger) : ViewComponent
{
    /// <summary>Omits inactive-account counts while leaving service authorization in effect.</summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new SocialNavigationViewModel();
        if (User.Identity?.IsAuthenticated == true)
        {
            try
            {
                model.PendingRequests = await requests.GetPendingCountAsync();
                model.UnreadNotifications = await notifications.GetUnreadCountAsync();
            }
            catch (DomainRuleException) { /* The destination actions still reject inactive accounts. */ }
            catch (Exception exception)
            {
                // Keep the shared error screen renderable when a counter's infrastructure is unavailable.
                logger.LogWarning(exception, "Social navigation counters could not be loaded.");
            }
        }
        return View(model);
    }
}
