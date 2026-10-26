using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.ViewComponents
{
    public class FriendshipRequestMenuViewComponent : ViewComponent
    {
        private readonly IFriendshipRequestService _friendshipRequestService;
        private readonly UserManager<AppUser> _userManager;

        public FriendshipRequestMenuViewComponent(
            IFriendshipRequestService friendshipRequestService,
            UserManager<AppUser> userManager)
        {
            _friendshipRequestService = friendshipRequestService;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (HttpContext.User.Identity?.IsAuthenticated != true)
                return Content(string.Empty);

            AppUser? user = await _userManager.GetUserAsync(HttpContext.User);
            if (user?.EmailConfirmed != true) return Content(string.Empty);

            ServiceResultWithData<int> result =
                await _friendshipRequestService.GetReceivedPendingCountAsync(user.Id);

            // No inventar un numero de solicitudes si ocurre un error de consulta.
            return View(result.IsSuccess ? result.Data : 0);
        }
    }
}