using System.Net;
using System.Text.RegularExpressions;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LinkUpPro.Tests;

/// <summary>Exercises actual Razor forms, authentication, activation, antiforgery, validation, and encoded text.</summary>
[Collection("SqlDatabase")]
public sealed class ModuleWebIntegrationTests(SqlDatabaseFixture database)
{
    [Theory]
    [InlineData("/Friends")]
    [InlineData("/FriendshipRequests")]
    [InlineData("/Notifications")]
    [InlineData("/Battleship")]
    public async Task ModuleRoutesRejectUnauthenticatedRequests(string route)
    {
        await using var env = new TestEnvironment(database);
        await using var factory = new ModuleWebFactory(database, env.Directory, env.Clock);
        using var client = CreateClient(factory);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);
    }

    [Theory]
    [InlineData("/Friends")]
    [InlineData("/FriendshipRequests")]
    [InlineData("/FriendshipRequests/New")]
    [InlineData("/Notifications")]
    [InlineData("/Battleship")]
    [InlineData("/Battleship/New")]
    public async Task ActiveAccountScreensRenderSuccessfully(string route)
    {
        await using var env = new TestEnvironment(database);
        await using var factory = new ModuleWebFactory(database, env.Directory, env.Clock);
        using var client = CreateClient(factory, env.Alice);
        var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Contains("Battleship", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PostWithoutAntiforgeryTokenCannotCreateARequest()
    {
        await using var env = new TestEnvironment(database);
        await using var factory = new ModuleWebFactory(database, env.Directory, env.Clock);
        using var client = CreateClient(factory, env.Alice);
        var response = await client.PostAsync("/FriendshipRequests/New", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["SelectedUserId"] = env.Bob }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await env.Context.FriendshipRequests.Where(r => r.SenderUserId == env.Alice).ToListAsync());
    }

    [Fact]
    public async Task ValidRequestFormIgnoresForgedSenderAndState()
    {
        await using var env = new TestEnvironment(database);
        await using var factory = new ModuleWebFactory(database, env.Directory, env.Clock);
        using var client = CreateClient(factory, env.Alice);
        var token = await TokenAsync(client, "/FriendshipRequests/New");
        var response = await client.PostAsync("/FriendshipRequests/New", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["SelectedUserId"] = env.Bob, ["SenderUserId"] = env.Charlie,
                ["Status"] = "Accepted", ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var check = database.CreateContext();
        var request = Assert.Single(await check.FriendshipRequests.Where(r => r.SenderUserId == env.Alice).ToListAsync());
        Assert.Equal(FriendshipRequestStatus.Pending, request.Status);
        Assert.Equal(env.Bob, request.ReceiverUserId);
    }

    [Fact]
    public async Task MissingSelectionReturnsValidationAndDoesNotCreateRequest()
    {
        await using var env = new TestEnvironment(database);
        await using var factory = new ModuleWebFactory(database, env.Directory, env.Clock);
        using var client = CreateClient(factory, env.Alice);
        var token = await TokenAsync(client, "/FriendshipRequests/New");
        var response = await client.PostAsync("/FriendshipRequests/New", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Debe seleccionar un usuario", await response.Content.ReadAsStringAsync());
        Assert.Empty(await env.Context.FriendshipRequests.Where(r => r.SenderUserId == env.Alice).ToListAsync());
    }

    [Fact]
    public async Task InactiveAuthenticatedAccountCannotUseModuleScreens()
    {
        await using var env = new TestEnvironment(database);
        await using var factory = new ModuleWebFactory(database, env.Directory, env.Clock);
        using var client = CreateClient(factory, env.Inactive);
        var response = await client.GetAsync("/Battleship");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("inactiva", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FriendPostsEncodeUserTextInsteadOfRenderingExecutableHtml()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var post = await env.AddPostAsync(env.Bob);
        post.Content = "<script>alert(1)</script>";
        await env.Context.SaveChangesAsync();
        await using var factory = new ModuleWebFactory(database, env.Directory, env.Clock);
        using var client = CreateClient(factory, env.Alice);
        var html = await client.GetStringAsync("/Friends");
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        // The friend feed renders the shared post card with reactions and the comment form.
        Assert.Contains($"id=\"post-{post.Id}\"", html);
        Assert.Contains("/Reaction/Toggle", html);
        Assert.Contains("/Comment/Create", html);
        var details = await client.GetStringAsync($"/Post/Details?postId={post.Id}");
        Assert.Contains("&lt;script&gt;", details);
        Assert.DoesNotContain("<script>alert(1)</script>", details);
    }

    [Fact]
    public async Task GameRendersExactly144AttackCellsAndNoOpponentsFleet()
    {
        await using var env = new TestEnvironment(database);
        var game = BattleshipRulesTests.ReadyGame(env.Alice, env.Bob);
        env.Context.BattleshipGames.Add(game);
        await env.Context.SaveChangesAsync();
        await using var factory = new ModuleWebFactory(database, env.Directory, env.Clock);
        using var client = CreateClient(factory, env.Alice);
        var html = await client.GetStringAsync("/Battleship/Game/" + game.Id);
        Assert.Equal(144, Regex.Matches(html, "aria-label=\"Atacar ").Count);
        Assert.DoesNotContain("is-ship", html);
        var denied = await client.GetAsync("/Battleship/OpponentBoard/" + game.Id);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task MutationActionsRejectGetRequests()
    {
        await using var env = new TestEnvironment(database);
        await using var factory = new ModuleWebFactory(database, env.Directory, env.Clock);
        using var client = CreateClient(factory, env.Alice);
        foreach (var route in new[] { "/Battleship/Attack", "/Battleship/Place", "/Battleship/Surrender",
            "/FriendshipRequests/Respond", "/Friends/Remove", "/Notifications/ReadAll" })
        {
            // Conventional routing may report either no matching action or an unsupported verb.
            var status = (await client.GetAsync(route)).StatusCode;
            Assert.Contains(status, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        }
    }

    /// <summary>Checks the setup screens and proves that posted winner data cannot override surrender rules.</summary>
    [Fact]
    public async Task SetupScreensAndSurrenderUseRealProtectedForms()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var id = await env.Get<IBattleshipService>().CreateAsync(env.Bob);
        await using var factory = new ModuleWebFactory(database, env.Directory, env.Clock);
        using var client = CreateClient(factory, env.Alice);
        Assert.Contains("Selecciona un barco", await client.GetStringAsync($"/Battleship/Game/{id}"));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/Battleship/SelectCell/{id}")).StatusCode);
        var grid = await client.GetStringAsync($"/Battleship/SelectCell/{id}?shipNumber=0");
        Assert.Equal(144, Regex.Matches(grid, "class=\"bs-cell").Count);
        var token = await TokenAsync(client, $"/Battleship/Direction/{id}?shipNumber=0&row=0&column=0");
        var response = await client.PostAsync("/Battleship/Place", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["Id"] = id.ToString(), ["Version"] = "1", ["ShipNumber"] = "0",
                ["Row"] = "0", ["Column"] = "0", ["Direction"] = "3", ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await using var check = database.CreateContext();
        Assert.Single(await check.Set<LinkUpPro.Core.Domain.Entities.ShipPlacement>().Where(s => s.GameId == id).ToListAsync());
        var game = await check.BattleshipGames.FindAsync(id);
        token = await TokenAsync(client, $"/Battleship/ConfirmSurrender/{id}");
        response = await client.PostAsync("/Battleship/Surrender", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["Id"] = id.ToString(), ["Version"] = game!.Version.ToString(),
                ["WinnerUserId"] = env.Alice, ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        check.ChangeTracker.Clear();
        Assert.Equal(env.Bob, (await check.BattleshipGames.FindAsync(id))!.WinnerUserId);
    }

    /// <summary>Checks the reserved publication route and verifies that a saved link cannot bypass changed privacy.</summary>
    [Fact]
    public async Task NotificationNavigationRechecksPrivacyBeforeRedirectingToThePublicationModule()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var post = await env.AddPostAsync(env.Alice);
        var notification = new LinkUpPro.Core.Domain.Entities.Notification
        {
            Id = 0, ActorUserId = env.Alice, RecipientUserId = env.Bob,
            Type = NotificationType.Reaction, Description = "An interaction", CreatedAt = env.Clock.Now.UtcDateTime,
            PostId = post.Id
        };
        env.Context.Notifications.Add(notification);
        await env.Context.SaveChangesAsync();
        await using var factory = new ModuleWebFactory(database, env.Directory, env.Clock);
        using var client = CreateClient(factory, env.Bob);
        var response = await client.GetAsync($"/Notifications/Open/{notification.Id}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Post/Details?postId={post.Id}#comments-{post.Id}", response.Headers.Location!.OriginalString);
        post.Privacy = PostPrivacy.OnlyMe;
        await env.Context.SaveChangesAsync();
        response = await client.GetAsync($"/Notifications/Open/{notification.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    /// <summary>Uses HTTPS so authentication and antiforgery cookies have their production-compatible scope.</summary>
    private static HttpClient CreateClient(ModuleWebFactory factory, string? userId = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        if (userId is not null) client.DefaultRequestHeaders.Add("X-Test-User", userId);
        return client;
    }

    /// <summary>Extracts a real Razor-generated antiforgery token while preserving its paired cookie.</summary>
    private static async Task<string> TokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var input = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>").Value;
        var token = Regex.Match(input, "value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        return WebUtility.HtmlDecode(token);
    }
}
