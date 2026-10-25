using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Application.ViewModels.Reaction;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{
    public class ReactionController : SocialController
    {
        private readonly IReactionService _reactionService;
        private readonly ICurrentUserService _currentUser;

        public ReactionController(IReactionService reactionService, ICurrentUserService currentUser)
        {
            _reactionService = reactionService;
            _currentUser = currentUser;
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(ToggleReactionViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return ErrorPage("Debe seleccionar una reacción válida.");
            }

            ServiceResult result = await _reactionService.ToggleReactionAsync(vm.PostId,
                _currentUser.GetRequiredUserId(), vm.ReactionType!.Value);

            if (!result.IsSuccess)
            {
                return FailurePage(result, "No se pudo procesar la reacción.");
            }

            // Return to the screen that sent the reaction (Home, Amigos, perfil o detalle).
            if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            {
                return LocalRedirect(vm.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
