using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.ViewModels.Comment;
using LinkUpPro.Core.Application.ViewModels.Home;
using LinkUpPro.Core.Application.ViewModels.Post;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IPostService _postService;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly ICommentService _commentService;

        public HomeController(IPostService postService, IMapper mapper, UserManager<AppUser> userManager,
            ICommentService commentService)
        {
            _postService = postService;
            _mapper = mapper;
            _userManager = userManager;
            _commentService = commentService;
        }

        public async Task<IActionResult> Index([FromQuery] PostFilterViewModel filter)
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

            if (filter.StartDate.HasValue && filter.EndDate.HasValue &&
                filter.StartDate.Value > filter.EndDate.Value)
            {
                ModelState.AddModelError("Filter.EndDate",
                    "La fecha final no puede ser anterior a la fecha inicial");
            }

            PostFilterDto? filterDto = ModelState.IsValid ? _mapper.Map<PostFilterDto>(filter) : null;

            var result = await _postService.GetMyPostsAsync(user.Id, filterDto);

            if (!result.IsSuccess || result.Data == null)
            {
                return Problem("No se pudieron cargar las publicaciones.");
            }

            List<PostViewModel> posts = _mapper.Map<List<PostViewModel>>(result.Data);

            foreach (PostViewModel post in posts)
            {
                post.UserName = user.UserName ?? string.Empty;
                post.UserProfilePicturePath = user.ProfilePicturePath;
            }

            if (posts.Count > 0)
            {
                var commentsResult = await _commentService.GetCommentsByPostIdsAsync(
                    posts.Select(p => p.Id),
                    user.Id);

                if (!commentsResult.IsSuccess || commentsResult.Data == null)
                {
                    return Problem("No se pudieron cargar los comentarios.");
                }

                foreach (PostViewModel post in posts)
                {
                    if (commentsResult.Data.TryGetValue(post.Id, out var comments))
                    {
                        post.Comments = _mapper.Map<List<CommentViewModel>>(comments);
                    }
                }

                List<CommentViewModel> allComments = GetAllComments(
                    posts.SelectMany(p => p.Comments)).ToList();

                string[] authorIds = allComments
                    .Where(c => !c.IsDeleted)
                    .Select(c => c.UserId)
                    .Distinct()
                    .ToArray();

                if (authorIds.Length > 0)
                {
                    var authors = await _userManager.Users
                        .AsNoTracking()
                        .Where(u => authorIds.Contains(u.Id))
                        .Select(u => new
                        {
                            u.Id,
                            u.UserName,
                            u.ProfilePicturePath
                        })
                        .ToDictionaryAsync(u => u.Id);

                    foreach (CommentViewModel comment in allComments)
                    {
                        if (comment.IsDeleted)
                        {
                            continue;
                        }

                        if (authors.TryGetValue(comment.UserId, out var author))
                        {
                            comment.UserName = author.UserName ?? string.Empty;
                            comment.UserProfilePicturePath = author.ProfilePicturePath;
                        }
                    }
                }
            }

            HomeViewModel vm = new()
            {
                Filter = filter,
                Posts = posts
            };

            ViewData["CommentError"] = TempData["CommentError"]?.ToString();
            ViewData["CommentPostId"] = TempData["CommentPostId"]?.ToString();
            ViewData["CommentParentId"] = TempData["CommentParentId"]?.ToString();
            ViewData["CommentContent"] = TempData["CommentContent"]?.ToString();
            ViewData["CurrentUserId"] = user.Id;

            return View(vm);
        }

        private static IEnumerable<CommentViewModel> GetAllComments(IEnumerable<CommentViewModel> comments)
        {
            foreach (CommentViewModel comment in comments)
            {
                yield return comment;

                foreach (CommentViewModel reply in GetAllComments(comment.Replies))
                {
                    yield return reply;
                }
            }
        }

        /// <summary>Returns a safe production error for failed requests without disclosing technical details.</summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult Error()
        {
            Response.StatusCode = 500;
            return View("~/Views/Shared/SocialError.cshtml", new LinkUpPro.ViewModels.Social.SocialErrorViewModel
                { Message = "Ocurrió un error al procesar la solicitud. Inténtelo nuevamente." });
        }
    }
}