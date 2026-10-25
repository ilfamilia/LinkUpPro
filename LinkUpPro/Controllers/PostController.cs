using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Application.ViewModels.Home;
using LinkUpPro.Core.Application.ViewModels.Post;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.Services;
using LinkUpPro.ViewModels.Social;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{
    public class PostController : SocialController
    {
        private readonly IPostService _postService;
        private readonly IImageStorageService _imageStorage;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;
        private readonly PostFeedBuilder _feedBuilder;

        public PostController(IPostService postService, IImageStorageService imageStorage,
            ICurrentUserService currentUser, IMapper mapper, PostFeedBuilder feedBuilder)
        {
            _postService = postService;
            _imageStorage = imageStorage;
            _currentUser = currentUser;
            _mapper = mapper;
            _feedBuilder = feedBuilder;
        }

        // Single post page used by notifications and friend feeds; visibility is rechecked on every request.
        [HttpGet]
        public async Task<IActionResult> Details(int postId)
        {
            List<PostCardViewModel> cards;

            try
            {
                // The publication service rechecks author/viewer activity, deletion, privacy and friendship.
                cards = await _feedBuilder.BuildAsync([postId]);
            }
            catch (DomainRuleException)
            {
                cards = [];
            }

            if (cards.Count == 0)
            {
                return ErrorPage("No posee permisos para visualizar esta publicación.", StatusCodes.Status403Forbidden);
            }

            ViewData["CommentError"] = TempData["CommentError"]?.ToString();
            ViewData["CommentPostId"] = TempData["CommentPostId"]?.ToString();
            ViewData["CommentParentId"] = TempData["CommentParentId"]?.ToString();
            ViewData["CommentContent"] = TempData["CommentContent"]?.ToString();
            ViewData["CurrentUserId"] = _feedBuilder.CurrentUserId;
            ViewData["FriendIds"] = await _feedBuilder.GetFriendIdsAsync();

            return View(cards[0]);
        }

        public IActionResult Create()
        {
            return RedirectToRoute(new { controller = "Home", action = "index" });
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreatePostViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return await ReturnHomeAsync(vm);
            }

            string userId = _currentUser.GetRequiredUserId();

            CreatePostDto dto = _mapper.Map<CreatePostDto>(vm);
            dto.UserId = userId;

            string? uploadedImagePath = null;

            if ((vm.MediaType == PostMediaType.Image && !string.IsNullOrWhiteSpace(vm.YouTubeUrl)) ||
            (vm.MediaType == PostMediaType.YouTubeVideo && vm.ImageFile != null))
            {
                ModelState.AddModelError(string.Empty, "No puedes incluir una imagen y un video simultáneamente.");

                return await ReturnHomeAsync(vm);
            }

            if (vm.MediaType == PostMediaType.Image)
            {
                if (vm.ImageFile == null || vm.ImageFile.Length == 0)
                {
                    ModelState.AddModelError(nameof(vm.ImageFile), "Debe seleccionar una imagen para crear la publicación.");

                    return await ReturnHomeAsync(vm);
                }

                try
                {
                    uploadedImagePath = await _imageStorage.SaveAsync(vm.ImageFile, userId, "Posts");

                    dto.ImagePath = uploadedImagePath;
                }
                catch (InvalidDataException ex)
                {
                    ModelState.AddModelError(nameof(vm.ImageFile), ex.Message);
                    return await ReturnHomeAsync(vm);
                }
                catch (IOException)
                {
                    ModelState.AddModelError(nameof(vm.ImageFile), "No fue posible guardar la imagen. Inténtelo nuevamente.");
                    return await ReturnHomeAsync(vm);
                }
            }

            ServiceResult result = await _postService.AddAsync(dto);

            if (result.IsSuccess)
            {
                return RedirectToAction("Index", "Home");
            }

            // Nothing was saved, so the uploaded image is not kept.
            _imageStorage.Delete(uploadedImagePath);

            if (result.Status == OperationStatus.Failed)
            {
                ModelState.AddModelError(string.Empty, "No se pudo crear la publicación. Inténtelo nuevamente.");
            }
            else
            {
                foreach (string error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }

            return await ReturnHomeAsync(vm);
        }

        public async Task<IActionResult> Edit(int postId)
        {
            var result = await _postService.GetMyPostByIdAsync(postId, _currentUser.GetRequiredUserId());

            if (!result.IsSuccess || result.Data == null)
            {
                return FailurePage(result, "No se pudo cargar la publicación.");
            }

            UpdatePostViewModel vm = _mapper.Map<UpdatePostViewModel>(result.Data);

            ViewData["PostId"] = postId;

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int postId, UpdatePostViewModel vm)
        {
            string userId = _currentUser.GetRequiredUserId();

            var postResult = await _postService.GetMyPostByIdAsync(postId, userId);

            if (!postResult.IsSuccess || postResult.Data == null)
            {
                return FailurePage(postResult, "No se pudo cargar la publicación.");
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
            dto.UserId = userId;

            string? uploadedImagePath = null;

            if (vm.MediaType == PostMediaType.Image && vm.ImageFile != null)
            {
                try
                {
                    uploadedImagePath = await _imageStorage.SaveAsync(vm.ImageFile, userId, "Posts");

                    dto.ImagePath = uploadedImagePath;
                }
                catch (InvalidDataException ex)
                {
                    ModelState.AddModelError(nameof(vm.ImageFile), ex.Message);

                    return ReturnEdit(vm, postId, previousImagePath);
                }
                catch (IOException)
                {
                    ModelState.AddModelError(nameof(vm.ImageFile), "No fue posible guardar la imagen. Inténtelo nuevamente.");

                    return ReturnEdit(vm, postId, previousImagePath);
                }
            }

            ServiceResult result = await _postService.UpdateAsync(dto, postId);

            if (result.IsSuccess)
            {
                // The previous image is removed only after the post no longer references it.
                if (!string.IsNullOrWhiteSpace(previousImagePath) &&
                    (vm.MediaType == PostMediaType.YouTubeVideo || !string.IsNullOrWhiteSpace(uploadedImagePath)))
                {
                    _imageStorage.Delete(previousImagePath);
                }

                return RedirectToAction("Index", "Home");
            }

            _imageStorage.Delete(uploadedImagePath);

            switch (result.Status)
            {
                case OperationStatus.Failed:
                    ModelState.AddModelError(string.Empty,
                        "No se pudo actualizar la publicación. Inténtelo nuevamente.");
                    break;

                case OperationStatus.ValidationError:
                case OperationStatus.Conflict:
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                    break;

                default:
                    return FailurePage(result, "No se pudo actualizar la publicación.");
            }

            return ReturnEdit(vm, postId, previousImagePath);
        }

        public async Task<IActionResult> Delete(int postId)
        {
            var result = await _postService.GetMyPostByIdAsync(postId, _currentUser.GetRequiredUserId());

            if (!result.IsSuccess || result.Data == null)
            {
                return FailurePage(result, "No se pudo cargar la publicación.");
            }

            DeletePostViewModel vm = new()
            {
                Id = result.Data.Id
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(DeletePostViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return ErrorPage("El ID de la publicación es inválido.");
            }

            ServiceResult result = await _postService.DeleteAsync(vm.Id, _currentUser.GetRequiredUserId());

            switch (result.Status)
            {
                case OperationStatus.Success:
                    return RedirectToAction("Index", "Home");

                case OperationStatus.Failed:
                    ModelState.AddModelError(string.Empty, "No se pudo eliminar la publicación. Inténtelo nuevamente.");
                    break;

                case OperationStatus.ValidationError:
                case OperationStatus.Conflict:
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                    break;

                default:
                    return FailurePage(result, "No se pudo eliminar la publicación.");
            }

            return View(vm);
        }

        private async Task<IActionResult> ReturnHomeAsync(CreatePostViewModel vm)
        {
            HomeViewModel homeVm = new()
            {
                CreatePost = vm,
                Posts = await _feedBuilder.BuildMyPostsAsync()
            };

            ViewData["CurrentUserId"] = _feedBuilder.CurrentUserId;
            ViewData["FriendIds"] = await _feedBuilder.GetFriendIdsAsync();

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
