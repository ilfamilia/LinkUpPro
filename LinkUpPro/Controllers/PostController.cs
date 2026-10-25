using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Application.ViewModels.Comment;
using LinkUpPro.Core.Application.ViewModels.Home;
using LinkUpPro.Core.Application.ViewModels.Post;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Helpers;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Controllers
{
    [Authorize]
    public class PostController : Controller
    {
        private readonly IPostService _postService;
        private readonly ICommentService _commentService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;
        private readonly LinkUpPro.Services.PostFeedBuilder _feedBuilder;

        public PostController(IPostService postService, ICommentService commentService,
            UserManager<AppUser> userManager, IMapper mapper, LinkUpPro.Services.PostFeedBuilder feedBuilder)
        {
            _postService = postService;
            _commentService = commentService;
            _userManager = userManager;
            _mapper = mapper;
            _feedBuilder = feedBuilder;
        }

        // Single post page used by notifications and friend feeds; visibility is rechecked on every request.
        [HttpGet]
        public async Task<IActionResult> Details(int postId)
        {
            string? userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            List<LinkUpPro.ViewModels.Social.PostCardViewModel> cards;

            try
            {
                // The publication service rechecks author/viewer activity, deletion, privacy and friendship.
                cards = await _feedBuilder.BuildAsync([postId]);
            }
            catch (LinkUpPro.Core.Domain.Exceptions.DomainRuleException)
            {
                cards = [];
            }

            if (cards.Count == 0)
            {
                Response.StatusCode = StatusCodes.Status403Forbidden;

                return View("~/Views/Shared/SocialError.cshtml", new LinkUpPro.ViewModels.Social.SocialErrorViewModel
                {
                    Message = "No posee permisos para visualizar esta publicación."
                });
            }

            ViewData["CommentError"] = TempData["CommentError"]?.ToString();
            ViewData["CommentPostId"] = TempData["CommentPostId"]?.ToString();
            ViewData["CommentParentId"] = TempData["CommentParentId"]?.ToString();
            ViewData["CommentContent"] = TempData["CommentContent"]?.ToString();
            ViewData["CurrentUserId"] = userId;
            ViewData["FriendIds"] = await _feedBuilder.GetFriendIdsAsync();

            return View(cards[0]);
        }

        public IActionResult Create()
        {
            return RedirectToRoute(new { controller = "Home", action = "index" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePostViewModel vm)
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
                return await ReturnHomeAsync(vm, user);
            }

            CreatePostDto dto = _mapper.Map<CreatePostDto>(vm);
            dto.UserId = user.Id;

            string? uploadedImagePath = null;

            if ((vm.MediaType == PostMediaType.Image && !string.IsNullOrWhiteSpace(vm.YouTubeUrl)) ||
            (vm.MediaType == PostMediaType.YouTubeVideo && vm.ImageFile != null))
            {
                ModelState.AddModelError(string.Empty, "No puedes incluir una imagen y un video simultáneamente.");

                return await ReturnHomeAsync(vm, user);
            }

            if (vm.MediaType == PostMediaType.Image)
            {
                if (vm.ImageFile == null || vm.ImageFile.Length == 0)
                {
                    ModelState.AddModelError(nameof(vm.ImageFile), "Debe seleccionar una imagen para crear la publicación.");

                    return await ReturnHomeAsync(vm, user);
                }

                try
                {
                    uploadedImagePath = FileManager.Upload(vm.ImageFile, user.Id, "Posts");

                    dto.ImagePath = uploadedImagePath;
                }
                catch (InvalidDataException ex)
                {
                    ModelState.AddModelError(nameof(vm.ImageFile), ex.Message);
                    return await ReturnHomeAsync(vm, user);
                }
            }


            ServiceResult result = await _postService.AddAsync(dto);

            switch (result.Status)
            {
                case OperationStatus.Success:
                    return RedirectToAction("Index", "Home");

                case OperationStatus.Failed:
                    ModelState.AddModelError(string.Empty, "No se pudo crear la publicación. Inténtelo nuevamente.");
                    break;

                default:
                    if (!string.IsNullOrWhiteSpace(uploadedImagePath))
                    {
                        FileManager.DeleteFile(uploadedImagePath);
                    }

                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }

                    break;
            }

            return await ReturnHomeAsync(vm, user);
        }

        public async Task<IActionResult> Edit(int postId)
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

            var result = await _postService.GetMyPostByIdAsync(postId, user.Id);

            switch (result.Status)
            {
                case OperationStatus.NotFound:
                    return NotFound(result.Errors.FirstOrDefault());

                case OperationStatus.Forbidden:
                    return StatusCode(403, result.Errors.FirstOrDefault());

                case OperationStatus.ValidationError:
                    return BadRequest(result.Errors.FirstOrDefault());

                case OperationStatus.Unauthorized:
                    return Challenge();

                case OperationStatus.Failed:
                    return Problem("No se pudo cargar la publicación.");

                case OperationStatus.Success:
                    if (result.Data == null)
                    {
                        return Problem("No se pudo cargar la publicación.");
                    }

                    UpdatePostViewModel vm = _mapper.Map<UpdatePostViewModel>(result.Data);

                    ViewData["PostId"] = postId;

                    return View(vm);

                default:
                    return Problem("Ocurrió un error inesperado.");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int postId, UpdatePostViewModel vm)
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

            var postResult = await _postService.GetMyPostByIdAsync(postId, user.Id);

            if (!postResult.IsSuccess || postResult.Data == null)
            {
                return postResult.Status switch
                {
                    OperationStatus.NotFound => NotFound(),
                    OperationStatus.Forbidden => Forbid(),
                    OperationStatus.Unauthorized => Challenge(),
                    OperationStatus.ValidationError => BadRequest(),
                    _ => Problem("No se pudo cargar la publicación.")
                };
            }

            string? previousImagePath = postResult.Data.ImagePath;

            if (!ModelState.IsValid)
            {
                return ReturnEdit(vm, postId, previousImagePath);
            }

            if (vm.MediaType == PostMediaType.YouTubeVideo && vm.ImageFile != null)
            {
                ModelState.AddModelError(nameof(vm.ImageFile),
                    "No puedes incluir una imagen en una publicación de YouTube.");

                return ReturnEdit(vm, postId, previousImagePath);
            }

            UpdatePostDto dto = _mapper.Map<UpdatePostDto>(vm);
            dto.UserId = user.Id;

            string? uploadedImagePath = null;

            if (vm.MediaType == PostMediaType.Image && vm.ImageFile != null)
            {
                try
                {
                    uploadedImagePath = FileManager.Upload(vm.ImageFile, user.Id, "Posts");

                    dto.ImagePath = uploadedImagePath;
                }
                catch (InvalidDataException ex)
                {
                    ModelState.AddModelError(nameof(vm.ImageFile), ex.Message);

                    return ReturnEdit(vm, postId, previousImagePath);
                }
            }

            ServiceResult result = await _postService.UpdateAsync(dto, postId);

            switch (result.Status)
            {
                case OperationStatus.Success:
                    if (!string.IsNullOrWhiteSpace(previousImagePath) &&
                        (vm.MediaType == PostMediaType.YouTubeVideo ||
                         !string.IsNullOrWhiteSpace(uploadedImagePath)))
                    {
                        FileManager.DeleteFile(previousImagePath);
                    }

                    return RedirectToAction("Index", "Home");

                case OperationStatus.Failed:
                    ModelState.AddModelError(string.Empty,
                        "No se pudo actualizar la publicación. Inténtelo nuevamente.");
                    break;

                default:
                    if (!string.IsNullOrWhiteSpace(uploadedImagePath))
                    {
                        FileManager.DeleteFile(uploadedImagePath);
                    }

                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }

                    break;
            }

            return ReturnEdit(vm, postId, previousImagePath);
        }

        public async Task<IActionResult> Delete(int postId)
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

            var result = await _postService.GetMyPostByIdAsync(postId, user.Id);

            switch (result.Status)
            {
                case OperationStatus.Success:
                    if (result.Data == null)
                    {
                        return Problem("No se pudo cargar la publicación.");
                    }

                    DeletePostViewModel vm = new()
                    {
                        Id = result.Data.Id
                    };

                    return View(vm);

                case OperationStatus.NotFound:
                    return NotFound();

                case OperationStatus.Forbidden:
                    return Forbid();

                case OperationStatus.Unauthorized:
                    return Challenge();

                case OperationStatus.ValidationError:
                    return BadRequest();

                default:
                    return Problem("No se pudo cargar la publicación.");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(DeletePostViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            AppUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            if (!user.EmailConfirmed)
            {
                return Forbid();
            }

            ServiceResult result = await _postService.DeleteAsync(vm.Id, user.Id);

            switch (result.Status)
            {
                case OperationStatus.Success:
                    return RedirectToAction("Index", "Home");

                case OperationStatus.NotFound:
                    return NotFound();

                case OperationStatus.Forbidden:
                    return Forbid();

                case OperationStatus.Unauthorized:
                    return Challenge();

                case OperationStatus.ValidationError:
                    return BadRequest(string.Join(" ", result.Errors));

                case OperationStatus.Failed:
                    ModelState.AddModelError(string.Empty, "No se pudo eliminar la publicación. Inténtelo nuevamente.");
                    break;

                default:
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                    break;
            }

            var postResult = await _postService.GetMyPostByIdAsync(vm.Id, user.Id);

            if (!postResult.IsSuccess || postResult.Data == null)
            {
                return Problem("No se pudo cargar la publicación.");
            }

            PostViewModel postVm = _mapper.Map<PostViewModel>(postResult.Data);

            return View(postVm);
        }

        private async Task<IActionResult> ReturnHomeAsync(CreatePostViewModel vm, AppUser user)
        {
            var result = await _postService.GetMyPostsAsync(user.Id);

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
                var commentsResult = await _commentService.GetCommentsByPostIdsAsync(posts.Select(p => p.Id), user.Id);

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

                List<CommentViewModel> allComments = [];

                Stack<CommentViewModel> pendingComments = new(posts.SelectMany(p => p.Comments));

                while (pendingComments.Count > 0)
                {
                    CommentViewModel comment = pendingComments.Pop();

                    allComments.Add(comment);

                    foreach (CommentViewModel reply in comment.Replies)
                    {
                        pendingComments.Push(reply);
                    }
                }

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

            HomeViewModel homeVm = new()
            {
                CreatePost = vm,
                Posts = posts
            };

            ViewData["CurrentUserId"] = user.Id;

            return View("~/Views/Home/Index.cshtml", homeVm);
        }

        private IActionResult ReturnEdit(UpdatePostViewModel vm, int postId, string? imagePath)
        {
            vm.CurrentImagePath = imagePath;

            ViewData["PostId"] = postId;

            return View("Edit", vm);
        }
    }
}