using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Comment;
using LinkUpPro.Core.Application.Dtos.Notification;
using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Application.ViewModels.Notification;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly IPostService _postService;
        private readonly ICommentService _commentService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public NotificationController(
            INotificationService notificationService,
            IPostService postService,
            ICommentService commentService,
            UserManager<AppUser> userManager,
            IMapper mapper)
        {
            _notificationService = notificationService;
            _postService = postService;
            _commentService = commentService;
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<List<NotificationDto>> result =
                await _notificationService.GetMineAsync(user.Id);

            if (!result.IsSuccess || result.Data == null)
                return HandleFailure(result);

            List<NotificationViewModel> notifications =
                _mapper.Map<List<NotificationViewModel>>(result.Data);

            NotificationIndexViewModel vm = new()
            {
                Notifications = notifications,
                UnreadCount = notifications.Count(notification => !notification.IsRead)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();
            if (id <= 0) return BadRequest();

            ServiceResult result = await _notificationService.MarkAsReadAsync(id, user.Id);
            if (!result.IsSuccess) return HandleFailure(result);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResult result = await _notificationService.MarkAllAsReadAsync(user.Id);
            if (!result.IsSuccess) return HandleFailure(result);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Open(int id)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();
            if (id <= 0) return BadRequest();

            ServiceResultWithData<List<NotificationDto>> notificationsResult =
                await _notificationService.GetMineAsync(user.Id);
            if (!notificationsResult.IsSuccess || notificationsResult.Data == null)
                return HandleFailure(notificationsResult);

            NotificationDto? notification = notificationsResult.Data
                .FirstOrDefault(item => item.Id == id);
            if (notification == null) return NotFound();

            ServiceResultWithData<PostDto> postResult =
                await _postService.GetVisiblePostByIdAsync(notification.PostId, user.Id);

            if (postResult.Status is OperationStatus.Unauthorized)
                return Challenge();
            if (postResult.Status is OperationStatus.Forbidden)
                return Forbid();
            if (postResult.Status == OperationStatus.Failed)
                return Problem("No fue posible consultar el contenido relacionado.");
            if (!postResult.IsSuccess || postResult.Data == null)
                return ContentUnavailable();

            if (notification.CommentId.HasValue)
            {
                ServiceResultWithData<List<CommentDto>> commentsResult =
                    await _commentService.GetCommentsByPostIdAsync(
                        notification.PostId, user.Id);

                if (commentsResult.Status is OperationStatus.Unauthorized)
                    return Challenge();
                if (commentsResult.Status is OperationStatus.Forbidden)
                    return Forbid();
                if (commentsResult.Status == OperationStatus.Failed)
                    return Problem("No fue posible consultar el comentario relacionado.");
                if (!commentsResult.IsSuccess || commentsResult.Data == null ||
                    !ContainsActiveComment(commentsResult.Data, notification.CommentId.Value))
                {
                    return ContentUnavailable();
                }
            }

            string? page = postResult.Data.UserId == user.Id
                ? Url.Action("Index", "Home")
                : Url.Action("Profile", "Friendship",
                    new { friendUserId = postResult.Data.UserId });

            if (string.IsNullOrWhiteSpace(page))
                return Problem("No fue posible generar el enlace del contenido.");

            string target = notification.CommentId.HasValue
                ? $"comment-{notification.CommentId.Value}"
                : $"post-{notification.PostId}";

            return LocalRedirect($"{page}#{target}");
        }

        private IActionResult ContentUnavailable()
        {
            TempData["NotificationWarning"] =
                "El contenido relacionado con esta notificación ya no se encuentra " +
                "disponible para usted.";
            return RedirectToAction(nameof(Index));
        }

        private static bool ContainsActiveComment(IEnumerable<CommentDto> comments, int id)
        {
            foreach (CommentDto comment in comments)
            {
                if (comment.Id == id) return !comment.IsDeleted;
                if (ContainsActiveComment(comment.Replies, id)) return true;
            }
            return false;
        }

        private IActionResult HandleFailure(ServiceResult result)
        {
            return result.Status switch
            {
                OperationStatus.Unauthorized => Challenge(),
                OperationStatus.Forbidden => Forbid(),
                OperationStatus.NotFound => NotFound(),
                OperationStatus.ValidationError => BadRequest(),
                OperationStatus.Conflict => Conflict(),
                _ => Problem("No se pudo completar la operación de notificaciones.")
            };
        }
    }
}
