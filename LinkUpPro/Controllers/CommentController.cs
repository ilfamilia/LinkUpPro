using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Comment;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Application.ViewModels.Comment;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{
    public class CommentController : SocialController
    {
        private readonly ICommentService _commentService;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;

        public CommentController(ICommentService commentService, ICurrentUserService currentUser, IMapper mapper)
        {
            _commentService = commentService;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCommentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                string error = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Los datos del comentario son inválidos";

                return RedirectToOrigin(vm, error);
            }

            CreateCommentDto dto = _mapper.Map<CreateCommentDto>(vm);
            dto.UserId = _currentUser.GetRequiredUserId();

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
            var result = await _commentService.GetMyCommentByIdAsync(commentId, _currentUser.GetRequiredUserId());

            if (!result.IsSuccess || result.Data == null)
            {
                return FailurePage(result, "No se pudo cargar el comentario.");
            }

            UpdateCommentViewModel vm = _mapper.Map<UpdateCommentViewModel>(result.Data);

            ViewData["CommentId"] = commentId;
            ViewData["ReturnUrl"] = SafeReturnUrl(returnUrl);

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int commentId, UpdateCommentViewModel vm, string? returnUrl)
        {
            string safeReturnUrl = SafeReturnUrl(returnUrl);

            ViewData["CommentId"] = commentId;
            ViewData["ReturnUrl"] = safeReturnUrl;

            if (commentId <= 0)
            {
                return ErrorPage("El ID del comentario es inválido.");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            UpdateCommentDto dto = _mapper.Map<UpdateCommentDto>(vm);
            dto.UserId = _currentUser.GetRequiredUserId();

            ServiceResult result = await _commentService.UpdateAsync(dto, commentId);

            switch (result.Status)
            {
                case OperationStatus.Success:
                    return LocalRedirect(safeReturnUrl);

                case OperationStatus.ValidationError:
                case OperationStatus.Conflict:
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                    break;

                case OperationStatus.Failed:
                    ModelState.AddModelError(string.Empty, "No se pudo actualizar el comentario");
                    break;

                default:
                    return FailurePage(result, "No se pudo actualizar el comentario.");
            }

            return View(vm);
        }

        public async Task<IActionResult> Delete(int commentId, string? returnUrl)
        {
            var result = await _commentService.GetMyCommentByIdAsync(commentId, _currentUser.GetRequiredUserId());

            if (!result.IsSuccess || result.Data == null)
            {
                return FailurePage(result, "No se pudo cargar el comentario.");
            }

            DeleteCommentViewModel vm = new()
            {
                Id = result.Data.Id,
                ReturnUrl = SafeReturnUrl(returnUrl)
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(DeleteCommentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return ErrorPage("El ID del comentario es inválido.");
            }

            string safeReturnUrl = SafeReturnUrl(vm.ReturnUrl);

            vm.ReturnUrl = safeReturnUrl;

            ServiceResult result = await _commentService.DeleteAsync(vm.Id, _currentUser.GetRequiredUserId());

            switch (result.Status)
            {
                case OperationStatus.Success:
                    return LocalRedirect(safeReturnUrl);

                case OperationStatus.ValidationError:
                case OperationStatus.Conflict:
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                    break;

                case OperationStatus.Failed:
                    ModelState.AddModelError(string.Empty, "No se pudo eliminar el comentario");
                    break;

                default:
                    return FailurePage(result, "No se pudo eliminar el comentario.");
            }

            return View(vm);
        }

        private string SafeReturnUrl(string? returnUrl)
        {
            return Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Home") ?? "/";
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

            string returnUrl = SafeReturnUrl(vm.ReturnUrl);

            returnUrl = returnUrl.Split('#')[0];

            return LocalRedirect($"{returnUrl}#comments-{vm.PostId}");
        }
    }
}
