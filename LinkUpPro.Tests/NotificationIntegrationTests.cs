using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Exceptions;
using Xunit;

namespace LinkUpPro.Tests;

/// <summary>Verifies notification recipients, private reading, soft-deleted targets, and no self-notifications.</summary>
[Collection("SqlDatabase")]
public sealed class NotificationIntegrationTests(SqlDatabaseFixture database)
{
    [Fact]
    public async Task CommentsAndRepliesNotifyTheCorrectAuthor()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        await env.MakeFriendsAsync(env.Alice, env.Charlie);
        var post = await env.AddPostAsync(env.Alice);
        var parent = await AddCommentAsync(env, post.Id, env.Charlie, null);
        env.Current.UserId = env.Bob;
        var comment = await AddCommentAsync(env, post.Id, env.Bob, null);
        var reply = await AddCommentAsync(env, post.Id, env.Bob, parent.Id);
        var notifications = env.Get<INotificationService>();
        await notifications.NotifyCommentAsync(comment.Id);
        await notifications.NotifyCommentAsync(reply.Id);
        env.Current.UserId = env.Alice;
        Assert.Equal(NotificationType.Comment, Assert.Single(await notifications.GetAllAsync()).Type);
        env.Current.UserId = env.Charlie;
        Assert.Equal(NotificationType.Reply, Assert.Single(await notifications.GetAllAsync()).Type);
    }

    [Fact]
    public async Task SelfCommentsAndSelfReactionsDoNotNotify()
    {
        await using var env = new TestEnvironment(database);
        var post = await env.AddPostAsync(env.Alice);
        var comment = await AddCommentAsync(env, post.Id, env.Alice, null);
        env.Context.Reactions.Add(new() { UserId = env.Alice, PostId = post.Id, Type = ReactionType.Like });
        await env.Context.SaveChangesAsync();
        var notifications = env.Get<INotificationService>();
        await notifications.NotifyCommentAsync(comment.Id);
        await notifications.NotifyReactionAsync(post.Id);
        Assert.Empty(await notifications.GetAllAsync());
    }

    [Fact]
    public async Task ReactionChangesNotifyButRemovalCannotCreateANotification()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var post = await env.AddPostAsync(env.Alice);
        var reaction = new Reaction { UserId = env.Bob, PostId = post.Id, Type = ReactionType.Like };
        env.Context.Reactions.Add(reaction);
        await env.Context.SaveChangesAsync();
        env.Current.UserId = env.Bob;
        var notifications = env.Get<INotificationService>();
        await notifications.NotifyReactionAsync(post.Id);
        reaction.Type = ReactionType.Dislike;
        await env.Context.SaveChangesAsync();
        await notifications.NotifyReactionAsync(post.Id);
        env.Context.Reactions.Remove(reaction);
        await env.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainRuleException>(() => notifications.NotifyReactionAsync(post.Id));
        env.Current.UserId = env.Alice;
        Assert.Equal(2, await notifications.GetUnreadCountAsync());
    }

    [Fact]
    public async Task OnlyRecipientCanReadAndUpdateNotifications()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var post = await env.AddPostAsync(env.Alice);
        var comment = await AddCommentAsync(env, post.Id, env.Bob, null);
        var notifications = env.Get<INotificationService>();
        env.Current.UserId = env.Bob;
        await notifications.NotifyCommentAsync(comment.Id);
        env.Current.UserId = env.Alice;
        var notification = Assert.Single(await notifications.GetAllAsync());
        env.Current.UserId = env.Charlie;
        await Assert.ThrowsAsync<DomainRuleException>(() => notifications.MarkReadAsync(notification.Id));
        await Assert.ThrowsAsync<DomainRuleException>(() => notifications.GetContentAsync(notification.Id));
        Assert.Empty(await notifications.GetAllAsync());
        await notifications.MarkAllReadAsync();
        env.Current.UserId = env.Alice;
        Assert.Equal(1, await notifications.GetUnreadCountAsync());
        await notifications.MarkReadAsync(notification.Id);
        Assert.Equal(0, await notifications.GetUnreadCountAsync());
    }

    [Fact]
    public async Task MarkAllUpdatesOnlyOwnedRowsAndRemovedContentRemainsInHistory()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var post = await env.AddPostAsync(env.Alice);
        var comment = await AddCommentAsync(env, post.Id, env.Bob, null);
        env.Current.UserId = env.Bob;
        var notifications = env.Get<INotificationService>();
        await notifications.NotifyCommentAsync(comment.Id);
        env.Current.UserId = env.Alice;
        var id = Assert.Single(await notifications.GetAllAsync()).Id;
        await notifications.MarkAllReadAsync();
        Assert.Equal(0, await notifications.GetUnreadCountAsync());
        post.IsDeleted = true;
        await env.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainRuleException>(() => notifications.GetContentAsync(id));
        Assert.Single(await notifications.GetAllAsync());
    }

    [Fact]
    public async Task NotificationLinkDoesNotBypassLostFriendshipOrOnlyMePrivacy()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var post = await env.AddPostAsync(env.Alice);
        var notification = new Notification { Id = 0, ActorUserId = env.Alice, RecipientUserId = env.Bob,
            Type = NotificationType.Reply, Description = "Reply", CreatedAt = env.Clock.Now.UtcDateTime, PostId = post.Id };
        env.Context.Notifications.Add(notification);
        await env.Context.SaveChangesAsync();
        env.Current.UserId = env.Bob;
        var service = env.Get<INotificationService>();
        Assert.Equal(post.Id, (await service.GetContentAsync(notification.Id)).PostId);
        post.Privacy = PostPrivacy.OnlyMe;
        await env.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainRuleException>(() => service.GetContentAsync(notification.Id));
        // A rolled-back operation clears tracked state, so reload before changing privacy again.
        var reloaded = await env.Context.Posts.FindAsync(post.Id);
        reloaded!.Privacy = PostPrivacy.FriendsOnly;
        await env.Context.SaveChangesAsync();
        await env.Get<IFriendshipService>().RemoveAsync(env.Alice);
        await Assert.ThrowsAsync<DomainRuleException>(() => service.GetContentAsync(notification.Id));
        Assert.Single(await service.GetAllAsync());
    }

    [Fact]
    public async Task InactiveReplyRecipientRetainsNotificationsForLaterActivation()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var post = await env.AddPostAsync(env.Alice);
        var parent = await AddCommentAsync(env, post.Id, env.Inactive, null);
        var reply = await AddCommentAsync(env, post.Id, env.Bob, parent.Id);
        env.Current.UserId = env.Bob;
        var service = env.Get<INotificationService>();
        await service.NotifyCommentAsync(reply.Id);
        env.Current.UserId = env.Inactive;
        await Assert.ThrowsAsync<DomainRuleException>(() => service.GetAllAsync());
        env.Directory.Users[3].IsActive = true;
        Assert.Equal(1, await service.GetUnreadCountAsync());
    }

    /// <summary>Seeds persisted interactions as the publication module would do before notifying.</summary>
    private static async Task<Comment> AddCommentAsync(TestEnvironment env, int postId, string userId, int? parent)
    {
        var comment = new Comment { Id = 0, PostId = postId, UserId = userId,
            Content = "Example comment", CreatedAt = env.Clock.Now.UtcDateTime, ParentCommentId = parent };
        env.Context.Comments.Add(comment);
        await env.Context.SaveChangesAsync();
        return comment;
    }
}
