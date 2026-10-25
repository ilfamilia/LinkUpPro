using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Application.ViewModels.Home;
using LinkUpPro.Core.Application.ViewModels.Post;
using LinkUpPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{
    public class HomeController : SocialController
    {
        private readonly IMapper _mapper;
        private readonly PostFeedBuilder _feedBuilder;

        public HomeController(IMapper mapper, PostFeedBuilder feedBuilder)
        {
            _mapper = mapper;
            _feedBuilder = feedBuilder;
        }

        public async Task<IActionResult> Index([FromQuery] PostFilterViewModel filter)
        {
            if (filter.StartDate.HasValue && filter.EndDate.HasValue &&
                filter.StartDate.Value > filter.EndDate.Value)
            {
                ModelState.AddModelError("Filter.EndDate",
                    "La fecha final no puede ser anterior a la fecha inicial");
            }

            PostFilterDto? filterDto = ModelState.IsValid ? _mapper.Map<PostFilterDto>(filter) : null;

            HomeViewModel vm = new()
            {
                Filter = filter,
                Posts = await _feedBuilder.BuildMyPostsAsync(filterDto)
            };

            ViewData["CommentError"] = TempData["CommentError"]?.ToString();
            ViewData["CommentPostId"] = TempData["CommentPostId"]?.ToString();
            ViewData["CommentParentId"] = TempData["CommentParentId"]?.ToString();
            ViewData["CommentContent"] = TempData["CommentContent"]?.ToString();
            ViewData["CurrentUserId"] = _feedBuilder.CurrentUserId;
            ViewData["FriendIds"] = await _feedBuilder.GetFriendIdsAsync();

            return View(vm);
        }

        /// <summary>Returns a safe production error for failed requests without disclosing technical details.</summary>
        // The exception handler re-executes failed POSTs here too, so the original form token is not re-validated.
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public IActionResult Error()
        {
            return ErrorPage("Ocurrió un error al procesar la solicitud. Inténtelo nuevamente.",
                StatusCodes.Status500InternalServerError);
        }
    }
}
