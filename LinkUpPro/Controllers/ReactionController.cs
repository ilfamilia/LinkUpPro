using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Application.ViewModels.Reaction;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{

    [Authorize]
    public class ReactionController : Controller
    {
        private readonly IReactionService _reactionService;
        private readonly UserManager<AppUser> _userManager;

        public ReactionController(IReactionService reactionService, UserManager<AppUser> userManager)
        {
            _reactionService = reactionService;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(ToggleReactionViewModel vm)
        {
            AppUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            if (!user.EmailConfirmed)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ServiceResult result = await _reactionService.ToggleReactionAsync(vm.PostId, user.Id,
                vm.ReactionType!.Value);

            switch (result.Status)
            {
                case OperationStatus.Success:
                    // Return to the screen that sent the reaction (Home, Amigos, perfil o detalle).
                    if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                    {
                        return LocalRedirect(vm.ReturnUrl);
                    }

                    return RedirectToAction("Index", "Home");

                case OperationStatus.ValidationError:
                    return BadRequest(result.Errors);

                case OperationStatus.NotFound:
                    return NotFound();

                case OperationStatus.Unauthorized:
                    return Challenge();

                case OperationStatus.Forbidden:
                    return Forbid();

                case OperationStatus.Conflict:
                    return Conflict(result.Errors);

                default:
                    return Problem("No se pudo procesar la reacción.");
            }
        }
    }
}
