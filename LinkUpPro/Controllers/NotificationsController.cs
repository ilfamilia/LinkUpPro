using AutoMapper;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.ViewModels.Social;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers;

/// <summary>Provides recipient-only notification reading and safe navigation to the publication module.</summary>
public sealed class NotificationsController(INotificationService notifications, IMapper mapper) : SocialController
{
    /// <summary>Lists the current recipient's notifications.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
        => View(mapper.Map<List<NotificationItemViewModel>>(await notifications.GetAllAsync()));

    /// <summary>Marks one owned notification as read.</summary>
    [HttpPost]
    public async Task<IActionResult> Read(int id)
    {
        await notifications.MarkReadAsync(id);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Marks every unread notification of the caller as read.</summary>
    [HttpPost]
    public async Task<IActionResult> ReadAll()
    {
        await notifications.MarkAllReadAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Rechecks target authorization and routes to the publication details page.</summary>
    [HttpGet]
    public async Task<IActionResult> Open(int id)
    {
        var target = await notifications.GetContentAsync(id);
        // The details page rechecks visibility again; the fragment scrolls to the related comment.
        var fragment = target.CommentId is null ? $"#comments-{target.PostId}" : $"#comment-{target.CommentId}";
        return LocalRedirect($"{Url.Action("Details", "Post", new { postId = target.PostId })}{fragment}");
    }
}
