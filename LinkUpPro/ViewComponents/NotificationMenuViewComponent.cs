using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.ViewComponents
{
    public class NotificationMenuViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<AppUser> _userManager;

        public NotificationMenuViewComponent(
            INotificationService notificationService,
            UserManager<AppUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (HttpContext.User.Identity?.IsAuthenticated != true)
                return Content(string.Empty);

            AppUser? user = await _userManager.GetUserAsync(HttpContext.User);
            if (user?.EmailConfirmed != true) return Content(string.Empty);

            ServiceResultWithData<int> result =
                await _notificationService.GetUnreadCountAsync(user.Id);

            return View(result.IsSuccess ? result.Data : 0);
        }
    }
}
