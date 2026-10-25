using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Social;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.Services;
using LinkUpPro.ViewModels.Social;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers;

/// <summary>Displays authorized friendship management and read-only friend publication integration.</summary>
public sealed class FriendsController(IFriendshipService friends, PostFeedBuilder feed, IMapper mapper) : SocialController
{
    /// <summary>Combines friend searching with independent, combinable publication filters.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(FriendPostFilterViewModel filter, string? friendSearch)
    {
        var all = await friends.GetFriendsAsync();
        var model = new FriendsPageViewModel
        {
            AllFriends = mapper.Map<List<FriendItemViewModel>>(all),
            Friends = mapper.Map<List<FriendItemViewModel>>(await friends.GetFriendsAsync(friendSearch)),
            Filter = filter, FriendSearch = friendSearch,
            AvailablePostCount = (await friends.GetFriendPostsAsync(new())).Count
        };
        if (ModelState.IsValid)
        {
            try
            {
                var visible = await friends.GetFriendPostsAsync(mapper.Map<FriendPostFilterDto>(filter));
                model.Posts = await feed.BuildAsync(visible.Select(p => p.Id));
            }
            catch (DomainRuleException exception)
            {
                // A stale friend selection is a filter error, not a page failure.
                ModelState.AddModelError(string.Empty, exception.Message);
            }
        }
        await PrepareFeedViewDataAsync();
        return View(model);
    }

    /// <summary>Authorizes a current friend profile before loading its visible publications.</summary>
    [HttpGet]
    public async Task<IActionResult> Profile(string id)
    {
        var profile = await friends.GetFriendProfileAsync(id);
        var mutual = await friends.GetMutualFriendsAsync(id);
        var model = new FriendsPageViewModel
        {
            Profile = mapper.Map<PublicUserViewModel>(profile), MutualFriendCount = mutual.Count,
            Posts = await feed.BuildAsync((await friends.GetFriendPostsAsync(new() { FriendId = id })).Select(p => p.Id))
        };
        await PrepareFeedViewDataAsync();
        return View(model);
    }

    /// <summary>Restores comment-form errors and exposes data the shared post card partials need.</summary>
    private async Task PrepareFeedViewDataAsync()
    {
        ViewData["CommentError"] = TempData["CommentError"]?.ToString();
        ViewData["CommentPostId"] = TempData["CommentPostId"]?.ToString();
        ViewData["CommentParentId"] = TempData["CommentParentId"]?.ToString();
        ViewData["CommentContent"] = TempData["CommentContent"]?.ToString();
        ViewData["CurrentUserId"] = feed.CurrentUserId;
        ViewData["FriendIds"] = await feed.GetFriendIdsAsync();
    }

    /// <summary>Lists only mutual friends who are also current active friends of the caller.</summary>
    [HttpGet]
    public async Task<IActionResult> Mutual(string id)
        => View(mapper.Map<List<PublicUserViewModel>>(await friends.GetMutualFriendsAsync(id)));

    /// <summary>Shows a confirmation page after authorizing the friendship.</summary>
    [HttpGet]
    public async Task<IActionResult> ConfirmRemove(string id)
    {
        var friend = await friends.GetFriendProfileAsync(id);
        return View(new SocialConfirmationViewModel { UserId = id, UserName = friend.UserName });
    }

    /// <summary>Logically removes an owned friendship using a protected POST request.</summary>
    [HttpPost]
    public async Task<IActionResult> Remove([FromForm] string userId)
    {
        await friends.RemoveAsync(userId);
        TempData["Success"] = "La amistad fue eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
