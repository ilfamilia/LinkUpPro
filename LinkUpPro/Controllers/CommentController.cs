using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Comment;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Application.ViewModels.Comment;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{

    [Authorize]
    public class CommentController : Controller
    {
        private readonly ICommentService _commentService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public CommentController(ICommentService commentService,
            UserManager<AppUser> userManager, IMapper mapper)
        {
            _commentService = commentService;
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCommentViewModel vm)
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
                string error = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Los datos del comentario son inválidos";

                return RedirectToOrigin(vm, error);
            }

            CreateCommentDto dto = _mapper.Map<CreateCommentDto>(vm);
            dto.UserId = user.Id;

            ServiceResult result = await _commentService.AddAsync(dto);

            switch (result.Status)
            {
                case OperationStatus.Success:
                    return RedirectToOrigin(vm);

                case OperationStatus.Unauthorized:
                    return Challenge();

                default:
                    string error = result.Errors.FirstOrDefault()
                        ?? "No se pudo registrar el comentario";

                    return RedirectToOrigin(vm, error);
            }
        }

        public async Task<IActionResult> Edit(int commentId, string? returnUrl)
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

            var result = await _commentService.GetMyCommentByIdAsync(commentId, user.Id);

            if (!result.IsSuccess || result.Data == null)
            {
                return result.Status switch
                {
                    OperationStatus.NotFound => NotFound(),
                    OperationStatus.Forbidden => Forbid(),
                    OperationStatus.Unauthorized => Challenge(),
                    OperationStatus.ValidationError => BadRequest(result.Errors),
                    _ => Problem("No se pudo cargar el comentario.")
                };
            }

            UpdateCommentViewModel vm = _mapper.Map<UpdateCommentViewModel>(result.Data);

            ViewData["CommentId"] = commentId;
            ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home");

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int commentId, UpdateCommentViewModel vm, string? returnUrl)
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

            string safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl!
                : Url.Action("Index", "Home") ?? "/";

            ViewData["CommentId"] = commentId;
            ViewData["ReturnUrl"] = safeReturnUrl;

            if (commentId <= 0)
            {
                return BadRequest("El ID del comentario es inválido");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            UpdateCommentDto dto = _mapper.Map<UpdateCommentDto>(vm);
            dto.UserId = user.Id;

            ServiceResult result = await _commentService.UpdateAsync(dto, commentId);

            switch (result.Status)
            {
                case OperationStatus.Success:
                    return LocalRedirect(safeReturnUrl);

                case OperationStatus.NotFound:
                    return NotFound();

                case OperationStatus.Unauthorized:
                    return Challenge();

                case OperationStatus.Forbidden:
                    return Forbid();

                case OperationStatus.ValidationError:
                case OperationStatus.Conflict:
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                    break;

                default:
                    ModelState.AddModelError(string.Empty,
                        "No se pudo actualizar el comentario");
                    break;
            }

            return View(vm);
        }

        public async Task<IActionResult> Delete(int commentId, string? returnUrl)
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

            var result = await _commentService.GetMyCommentByIdAsync(commentId, user.Id);

            if (!result.IsSuccess || result.Data == null)
            {
                return result.Status switch
                {
                    OperationStatus.NotFound => NotFound(),
                    OperationStatus.Forbidden => Forbid(),
                    OperationStatus.Unauthorized => Challenge(),
                    OperationStatus.ValidationError => BadRequest(result.Errors),
                    _ => Problem("No se pudo cargar el comentario.")
                };
            }

            DeleteCommentViewModel vm = new()
            {
                Id = result.Data.Id,
                ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home") ?? "/"
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(DeleteCommentViewModel vm)
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

            string safeReturnUrl = Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl! : Url.Action("Index", "Home") ?? "/";

            vm.ReturnUrl = safeReturnUrl;

            ServiceResult result = await _commentService.DeleteAsync(vm.Id, user.Id);

            switch (result.Status)
            {
                case OperationStatus.Success:
                    return LocalRedirect(safeReturnUrl);

                case OperationStatus.NotFound:
                    return NotFound();

                case OperationStatus.Unauthorized:
                    return Challenge();

                case OperationStatus.Forbidden:
                    return Forbid();

                case OperationStatus.ValidationError:
                case OperationStatus.Conflict:
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                    break;

                default:
                    ModelState.AddModelError(string.Empty, "No se pudo eliminar el comentario");
                    break;
            }

            return View(vm);
        }

        private IActionResult RedirectToOrigin(CreateCommentViewModel vm, string? error = null)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                TempData["CommentError"] = error;
                TempData["CommentPostId"] = vm.PostId.ToString();
                TempData["CommentParentId"] = vm.ParentCommentId?.ToString() ?? "";
                TempData["CommentContent"] = vm.Content?.Length > 500 ? vm.Content[..500] : vm.Content ?? string.Empty;
            }

            string returnUrl = Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl! : Url.Action("Index", "Home") ?? "/";

            returnUrl = returnUrl.Split('#')[0];

            return LocalRedirect($"{returnUrl}#comments-{vm.PostId}");
        }
    }
}
