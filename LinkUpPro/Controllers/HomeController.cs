using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.ViewModels.Home;
using LinkUpPro.Core.Application.ViewModels.Post;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IPostService _postService;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(IPostService postService, IMapper mapper, UserManager<AppUser> userManager)
        {
            _postService = postService;
            _mapper = mapper;
            _userManager = userManager;
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

            HomeViewModel vm = new()
            {
                Filter = filter,
                Posts = posts
            };

            return View(vm);
        }
    }
}