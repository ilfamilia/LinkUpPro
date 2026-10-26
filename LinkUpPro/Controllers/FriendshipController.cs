using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Friendship;
using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Application.ViewModels.Comment;
using LinkUpPro.Core.Application.ViewModels.Friendship;
using LinkUpPro.Core.Application.ViewModels.Post;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Controllers
{
    [Authorize]
    public class FriendshipController : Controller
    {
        private readonly IFriendshipService _friendshipService;
        private readonly IPostService _postService;
        private readonly ICommentService _commentService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public FriendshipController(IFriendshipService friendshipService,
            IPostService postService, ICommentService commentService,
            UserManager<AppUser> userManager, IMapper mapper)
        {
            _friendshipService = friendshipService;
            _postService = postService;
            _commentService = commentService;
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] FriendshipIndexQueryViewModel query)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<FriendshipSummaryDto> summaryResult =
                await _friendshipService.GetSummaryAsync(user.Id);
            if (!summaryResult.IsSuccess || summaryResult.Data == null)
                return ReadFailure(summaryResult);

            ServiceResultWithData<List<FriendDto>> friendsResult =
                await _friendshipService.GetFriendsAsync(user.Id);
            if (!friendsResult.IsSuccess) return ReadFailure(friendsResult);

            List<FriendViewModel> friends = _mapper.Map<List<FriendViewModel>>(
                friendsResult.Data ?? []);

            // Búsqueda de la lista, separada de los filtros de publicaciones
            List<FriendViewModel> filteredFriends = friends;
            if (!string.IsNullOrWhiteSpace(query.FriendSearch))
            {
                string term = query.FriendSearch.Trim();
                filteredFriends = friends.Where(friend =>
                    friend.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    friend.LastName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    friend.UserName.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (query.Filter.StartDate.HasValue && query.Filter.EndDate.HasValue &&
                query.Filter.StartDate.Value > query.Filter.EndDate.Value)
            {
                ModelState.AddModelError("Filter.EndDate",
                    "La fecha inicial no puede ser posterior a la fecha final.");
            }

            PostFilterDto? filter = ModelState.IsValid
                ? _mapper.Map<PostFilterDto>(query.Filter)
                : null;

            ServiceResultWithData<List<PostDto>> postsResult =
                await _postService.GetFriendsPostsAsync(user.Id, filter);

            if (!postsResult.IsSuccess && postsResult.Status == OperationStatus.ValidationError)
            {
                foreach (string error in postsResult.Errors)
                    ModelState.AddModelError("Filter.FriendUserId", error);
                postsResult = await _postService.GetFriendsPostsAsync(user.Id);
            }

            if (!postsResult.IsSuccess) return ReadFailure(postsResult);

            List<PostViewModel> posts = _mapper.Map<List<PostViewModel>>(postsResult.Data ?? []);
            if (!await PopulatePostDetailsAsync(posts, user.Id))
                return Problem("No se pudieron cargar los comentarios de las publicaciones.");

            PreparePostViews(user.Id, friends);

            ViewData["CommentError"] = TempData["CommentError"]?.ToString();
            ViewData["CommentPostId"] = TempData["CommentPostId"]?.ToString();
            ViewData["CommentParentId"] = TempData["CommentParentId"]?.ToString();
            ViewData["CommentContent"] = TempData["CommentContent"]?.ToString();

            FriendshipIndexViewModel vm = new()
            {
                Summary = _mapper.Map<FriendshipSummaryViewModel>(summaryResult.Data),
                Filter = query.Filter,
                FriendSearch = query.FriendSearch,
                Friends = friends,
                FilteredFriends = filteredFriends,
                Posts = posts
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Profile(string friendUserId)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<FriendDto> friendResult =
                await _friendshipService.GetFriendAsync(user.Id, friendUserId);
            if (!friendResult.IsSuccess || friendResult.Data == null)
                return ReadFailure(friendResult);

            ServiceResultWithData<List<PostDto>> postsResult =
                await _postService.GetFriendsPostsAsync(user.Id, new PostFilterDto
                {
                    FriendUserId = friendUserId
                });
            if (!postsResult.IsSuccess) return ReadFailure(postsResult);

            List<PostViewModel> posts = _mapper.Map<List<PostViewModel>>(postsResult.Data ?? []);
            if (!await PopulatePostDetailsAsync(posts, user.Id))
                return Problem("No se pudieron cargar los comentarios de este perfil.");

            // Únicamente los amigos actuales podrán tener enlaces de perfil
            ServiceResultWithData<List<FriendDto>> allFriends =
                await _friendshipService.GetFriendsAsync(user.Id);
            if (!allFriends.IsSuccess) return ReadFailure(allFriends);
            PreparePostViews(user.Id, _mapper.Map<List<FriendViewModel>>(allFriends.Data ?? []));

            ViewData["CommentError"] = TempData["CommentError"]?.ToString();
            ViewData["CommentPostId"] = TempData["CommentPostId"]?.ToString();
            ViewData["CommentParentId"] = TempData["CommentParentId"]?.ToString();
            ViewData["CommentContent"] = TempData["CommentContent"]?.ToString();

            return View(new FriendshipProfileViewModel
            {
                Friend = _mapper.Map<FriendViewModel>(friendResult.Data),
                Posts = posts
            });
        }

        [HttpGet]
        public async Task<IActionResult> MutualFriends(string friendUserId)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<FriendDto> friendResult =
                await _friendshipService.GetFriendAsync(user.Id, friendUserId);
            if (!friendResult.IsSuccess || friendResult.Data == null)
                return ReadFailure(friendResult);

            ServiceResultWithData<List<FriendDto>> mutualResult =
                await _friendshipService.GetMutualFriendsAsync(user.Id, friendUserId);
            if (!mutualResult.IsSuccess) return ReadFailure(mutualResult);

            return View(new FriendshipMutualFriendsViewModel
            {
                Friend = _mapper.Map<FriendViewModel>(friendResult.Data),
                MutualFriends = _mapper.Map<List<FriendViewModel>>(mutualResult.Data ?? [])
            });
        }

        [HttpGet]
        public async Task<IActionResult> Remove(string friendUserId)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<FriendDto> result =
                await _friendshipService.GetFriendAsync(user.Id, friendUserId);
            if (!result.IsSuccess || result.Data == null) return ReadFailure(result);

            return View(new FriendshipRemoveViewModel
            {
                FriendUserId = result.Data.Id,
                UserName = result.Data.UserName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(FriendshipRemoveViewModel vm)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            ServiceResult result = await _friendshipService.RemoveAsync(user.Id, vm.FriendUserId);
            if (result.IsSuccess)
            {
                TempData["FriendshipSuccess"] = "La amistad fue eliminada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            if (result.Status is OperationStatus.Unauthorized or OperationStatus.Forbidden or
                OperationStatus.NotFound)
            {
                return ReadFailure(result);
            }

            TempData["FriendshipError"] = string.Join(" ", result.Errors.Count > 0
                ? result.Errors : ["No fue posible eliminar la amistad."]);
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> PopulatePostDetailsAsync(List<PostViewModel> posts, string currentUserId)
        {
            if (posts.Count == 0) return true;

            ServiceResultWithData<Dictionary<int, List<LinkUpPro.Core.Application.Dtos.Comment.CommentDto>>> commentResult =
                await _commentService.GetCommentsByPostIdsAsync(
                    posts.Select(post => post.Id), currentUserId);
            if (!commentResult.IsSuccess || commentResult.Data == null) return false;

            foreach (PostViewModel post in posts)
            {
                if (commentResult.Data.TryGetValue(post.Id, out var comments))
                    post.Comments = _mapper.Map<List<CommentViewModel>>(comments);
            }

            string[] authorIds = posts.Select(post => post.UserId)
                .Concat(EnumerateComments(posts.SelectMany(post => post.Comments))
                    .Where(comment => !comment.IsDeleted)
                    .Select(comment => comment.UserId))
                .Distinct()
                .ToArray();

            if (authorIds.Length == 0) return true;

            var authors = await _userManager.Users
                .AsNoTracking()
                .Where(user => authorIds.Contains(user.Id))
                .Select(user => new { user.Id, user.UserName, user.ProfilePicturePath })
                .ToDictionaryAsync(user => user.Id);

            foreach (PostViewModel post in posts)
            {
                if (authors.TryGetValue(post.UserId, out var author))
                {
                    post.UserName = author.UserName ?? string.Empty;
                    post.UserProfilePicturePath = author.ProfilePicturePath;
                }
            }

            foreach (CommentViewModel comment in
                EnumerateComments(posts.SelectMany(post => post.Comments)))
            {
                if (!comment.IsDeleted && authors.TryGetValue(comment.UserId, out var author))
                {
                    comment.UserName = author.UserName ?? string.Empty;
                    comment.UserProfilePicturePath = author.ProfilePicturePath;
                }
            }

            return true;
        }

        private void PreparePostViews(string currentUserId, List<FriendViewModel> friends)
        {
            ViewData["CurrentUserId"] = currentUserId;
            ViewData["FriendIds"] = friends.Select(friend => friend.Id)
                .ToHashSet(StringComparer.Ordinal);
        }

        private static IEnumerable<CommentViewModel> EnumerateComments(
            IEnumerable<CommentViewModel> comments)
        {
            foreach (CommentViewModel comment in comments)
            {
                yield return comment;
                foreach (CommentViewModel reply in EnumerateComments(comment.Replies))
                    yield return reply;
            }
        }

        private IActionResult ReadFailure(ServiceResult result)
        {
            return result.Status switch
            {
                OperationStatus.Unauthorized => Challenge(),
                OperationStatus.Forbidden => Forbid(),
                OperationStatus.NotFound => NotFound(),
                OperationStatus.ValidationError => BadRequest(result.Errors),
                _ => Problem("No fue posible completar la consulta de amistades.")
            };
        }
    }
}
