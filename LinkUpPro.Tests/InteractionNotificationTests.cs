using LinkUpPro.Core.Application.Dtos.Comment;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Enums;
using Xunit;

namespace LinkUpPro.Tests;

/// <summary>Verifies that the publication module's comment and reaction services trigger notifications.</summary>
[Collection("SqlDatabase")]
public sealed class InteractionNotificationTests(SqlDatabaseFixture database)
{
    [Fact]
    public async Task CommentServiceNotifiesPostAuthorAndReplyNotifiesCommentAuthor()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        await env.MakeFriendsAsync(env.Alice, env.Charlie);
        var post = await env.AddPostAsync(env.Alice);
        var comments = env.Get<ICommentService>();

        env.Current.UserId = env.Bob;
        var result = await comments.AddAsync(new CreateCommentDto { PostId = post.Id, UserId = env.Bob, Content = "Hola" });
        Assert.Equal(OperationStatus.Success, result.Status);
        var bobComment = env.Context.Comments.Single(c => c.PostId == post.Id && c.UserId == env.Bob);

        env.Current.UserId = env.Charlie;
        result = await comments.AddAsync(new CreateCommentDto
            { PostId = post.Id, UserId = env.Charlie, Content = "Respuesta", ParentCommentId = bobComment.Id });
        Assert.Equal(OperationStatus.Success, result.Status);

        var notifications = env.Get<INotificationService>();
        env.Current.UserId = env.Alice;
        var aliceNotification = Assert.Single(await notifications.GetAllAsync());
        Assert.Equal(NotificationType.Comment, aliceNotification.Type);
        Assert.Equal(env.Bob, aliceNotification.Actor.Id);

        env.Current.UserId = env.Bob;
        var bobNotification = Assert.Single(await notifications.GetAllAsync());
        Assert.Equal(NotificationType.Reply, bobNotification.Type);
        Assert.Equal(env.Charlie, bobNotification.Actor.Id);
    }

    [Fact]
    public async Task OwnCommentOnOwnPostDoesNotNotify()
    {
        await using var env = new TestEnvironment(database);
        var post = await env.AddPostAsync(env.Alice);
        var result = await env.Get<ICommentService>().AddAsync(
            new CreateCommentDto { PostId = post.Id, UserId = env.Alice, Content = "Mi comentario" });
        Assert.Equal(OperationStatus.Success, result.Status);
        Assert.Empty(await env.Get<INotificationService>().GetAllAsync());
    }

    [Fact]
    public async Task ReactionRegistrationAndChangeNotifyButRemovalDoesNot()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var post = await env.AddPostAsync(env.Alice);
        var reactions = env.Get<IReactionService>();
        var notifications = env.Get<INotificationService>();

        env.Current.UserId = env.Bob;
        Assert.Equal(OperationStatus.Success, (await reactions.ToggleReactionAsync(post.Id, env.Bob, ReactionType.Like)).Status);
        Assert.Equal(OperationStatus.Success, (await reactions.ToggleReactionAsync(post.Id, env.Bob, ReactionType.Dislike)).Status);
        Assert.Equal(OperationStatus.Success, (await reactions.ToggleReactionAsync(post.Id, env.Bob, ReactionType.Dislike)).Status);

        env.Current.UserId = env.Alice;
        var list = await notifications.GetAllAsync();
        Assert.Equal(2, list.Count);
        Assert.All(list, n => Assert.Equal(NotificationType.Reaction, n.Type));
        Assert.Contains(list, n => n.Description.Contains("No me gusta"));
        Assert.Empty(env.Context.Reactions.Where(r => r.PostId == post.Id));
    }

    [Fact]
    public async Task FailedCommentOnInvisiblePostCreatesNoNotification()
    {
        await using var env = new TestEnvironment(database);
        var post = await env.AddPostAsync(env.Alice);
        env.Current.UserId = env.Bob;
        var result = await env.Get<ICommentService>().AddAsync(
            new CreateCommentDto { PostId = post.Id, UserId = env.Bob, Content = "Sin amistad" });
        Assert.NotEqual(OperationStatus.Success, result.Status);
        env.Current.UserId = env.Alice;
        Assert.Empty(await env.Get<INotificationService>().GetAllAsync());
    }
}
