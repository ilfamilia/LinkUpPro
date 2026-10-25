using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LinkUpPro.Tests;

/// <summary>Exercises real SQL transactions, canonical pair constraints, authorization, and preserved history.</summary>
[Collection("SqlDatabase")]
public sealed class FriendshipIntegrationTests(SqlDatabaseFixture database)
{
    [Fact]
    public async Task AcceptCreatesOneFriendshipAndHidingHistoryDoesNotDeleteIt()
    {
        await using var env = new TestEnvironment(database);
        var requests = env.Get<IFriendshipRequestService>();
        await requests.SendAsync(env.Bob);
        var request = Assert.Single(await requests.GetSentAsync());
        env.Current.UserId = env.Bob;
        await requests.AcceptAsync(request.Id);
        Assert.True(await env.Get<IFriendshipService>().AreFriendsAsync(env.Alice, env.Bob));
        Assert.Empty(await requests.GetIncomingAsync());
        env.Current.UserId = env.Alice;
        await requests.HideAsync(request.Id);
        Assert.Empty(await requests.GetSentAsync());
        Assert.True(await env.Get<IFriendshipService>().AreFriendsAsync(env.Alice, env.Bob));
        var stored = await env.Context.FriendshipRequests.FindAsync(request.Id);
        Assert.Equal(FriendshipRequestStatus.Accepted, stored!.Status);
        Assert.False(stored.IsVisibleToSender);
    }

    [Fact]
    public async Task RemovalAndNewAcceptanceReactivateTheSamePair()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var friends = env.Get<IFriendshipService>();
        await friends.RemoveAsync(env.Bob);
        Assert.False(await friends.AreFriendsAsync(env.Alice, env.Bob));
        var requests = env.Get<IFriendshipRequestService>();
        await requests.SendAsync(env.Bob);
        var id = Assert.Single(await requests.GetSentAsync()).Id;
        env.Current.UserId = env.Bob;
        await requests.AcceptAsync(id);
        Assert.True(await friends.AreFriendsAsync(env.Alice, env.Bob));
        Assert.Single(await env.Context.Friendships.Where(f => f.User1Id == env.Alice && f.User2Id == env.Bob).ToListAsync());
    }

    [Fact]
    public async Task RejectionAndCancellationPreserveSeparateHistory()
    {
        await using var env = new TestEnvironment(database);
        var requests = env.Get<IFriendshipRequestService>();
        await requests.SendAsync(env.Bob);
        var first = Assert.Single(await requests.GetSentAsync()).Id;
        env.Current.UserId = env.Bob;
        await requests.RejectAsync(first);
        env.Current.UserId = env.Alice;
        await requests.SendAsync(env.Bob);
        var second = (await requests.GetSentAsync()).Single(r => r.Status == FriendshipRequestStatus.Pending).Id;
        await requests.CancelAsync(second);
        var visible = Assert.Single(await requests.GetSentAsync());
        Assert.Equal(FriendshipRequestStatus.Rejected, visible.Status);
        Assert.Equal(2, await env.Context.FriendshipRequests.CountAsync(r => r.SenderUserId == env.Alice));
        Assert.False(await env.Get<IFriendshipService>().AreFriendsAsync(env.Alice, env.Bob));
    }

    [Fact]
    public async Task RequestsRejectSelfInactiveCurrentFriendAndBothDirectionDuplicates()
    {
        await using var env = new TestEnvironment(database);
        var requests = env.Get<IFriendshipRequestService>();
        await Assert.ThrowsAsync<DomainRuleException>(() => requests.SendAsync(env.Alice));
        await Assert.ThrowsAsync<DomainRuleException>(() => requests.SendAsync(env.Inactive));
        await env.MakeFriendsAsync(env.Alice, env.Charlie);
        await Assert.ThrowsAsync<DomainRuleException>(() => requests.SendAsync(env.Charlie));
        await requests.SendAsync(env.Bob);
        await Assert.ThrowsAsync<DomainRuleException>(() => requests.SendAsync(env.Bob));
        env.Current.UserId = env.Bob;
        await Assert.ThrowsAsync<DomainRuleException>(() => requests.SendAsync(env.Alice));
    }

    [Fact]
    public async Task OnlyReceiverRespondsAndOnlySenderCancelsOrHides()
    {
        await using var env = new TestEnvironment(database);
        var requests = env.Get<IFriendshipRequestService>();
        await requests.SendAsync(env.Bob);
        var id = Assert.Single(await requests.GetSentAsync()).Id;
        await Assert.ThrowsAsync<DomainRuleException>(() => requests.AcceptAsync(id));
        env.Current.UserId = env.Charlie;
        await Assert.ThrowsAsync<DomainRuleException>(() => requests.RejectAsync(id));
        await Assert.ThrowsAsync<DomainRuleException>(() => requests.CancelAsync(id));
        await Assert.ThrowsAsync<DomainRuleException>(() => requests.HideAsync(id));
        env.Current.UserId = env.Bob;
        Assert.Equal(1, await requests.GetPendingCountAsync());
        env.Directory.Users[0].IsActive = false;
        Assert.Equal(0, await requests.GetPendingCountAsync());
        await Assert.ThrowsAsync<DomainRuleException>(() => requests.AcceptAsync(id));
    }

    [Fact]
    public async Task EligibilityAndMutualFriendsExcludeInactiveOrAlreadyRelatedAccounts()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Charlie);
        await env.MakeFriendsAsync(env.Bob, env.Charlie);
        var eligible = Assert.Single(await env.Get<IFriendshipRequestService>().GetEligibleUsersAsync(" bOB "));
        Assert.Equal(env.Bob, eligible.User.Id);
        Assert.Equal(1, eligible.MutualFriendCount);
        var mutual = Assert.Single(await env.Get<IFriendshipService>().GetMutualFriendsAsync(env.Bob));
        Assert.Equal(env.Charlie, mutual.Id);
        env.Directory.Users[2].IsActive = false;
        Assert.Empty(await env.Get<IFriendshipService>().GetMutualFriendsAsync(env.Bob));
    }

    [Fact]
    public async Task PublicationFiltersRespectPrivacyDatesAndCurrentFriendship()
    {
        await using var env = new TestEnvironment(database);
        await env.MakeFriendsAsync(env.Alice, env.Bob);
        var visible = await env.AddPostAsync(env.Bob);
        visible.Content = "Hello searchable WORLD";
        visible.CreatedAt = env.Clock.Now.UtcDateTime.AddHours(23).AddMinutes(59);
        var hidden = await env.AddPostAsync(env.Bob);
        hidden.Privacy = PostPrivacy.OnlyMe;
        var deleted = await env.AddPostAsync(env.Bob);
        deleted.IsDeleted = true;
        await env.Context.SaveChangesAsync();
        var friends = env.Get<IFriendshipService>();
        var result = await friends.GetFriendPostsAsync(new() { Search = " world ", From = env.Clock.Now.Date,
            Until = env.Clock.Now.Date, MediaType = PostMediaType.Image, IsEdited = false, FriendId = env.Bob });
        Assert.Equal(visible.Id, Assert.Single(result).Id);
        await Assert.ThrowsAsync<DomainRuleException>(() => friends.GetFriendPostsAsync(new()
            { From = env.Clock.Now.Date.AddDays(1), Until = env.Clock.Now.Date }));
        await friends.RemoveAsync(env.Bob);
        Assert.Empty(await friends.GetFriendPostsAsync(new()));
        await Assert.ThrowsAsync<DomainRuleException>(() => friends.GetFriendProfileAsync(env.Bob));
    }

    [Fact]
    public async Task FailedAtomicOperationRollsBackAllEarlierRepositorySaves()
    {
        await using var env = new TestEnvironment(database);
        var atomic = env.Get<LinkUpPro.Core.Domain.Interfaces.IAtomicOperation>();
        var requests = env.Get<IFriendshipRequestService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => atomic.ExecuteAsync<bool>(async () =>
        {
            await requests.SendAsync(env.Bob);
            throw new InvalidOperationException("Simulated downstream failure.");
        }));
        Assert.Empty(await requests.GetSentAsync());
    }

    [Fact]
    public async Task ConcurrentCrossedRequestsProduceOnlyOnePendingRecord()
    {
        await using var first = new TestEnvironment(database);
        await using var second = new TestEnvironment(database, first.Directory, first.Bob, first.Clock);
        var outcomes = await Task.WhenAll(
            AttemptAsync(() => first.Get<IFriendshipRequestService>().SendAsync(first.Bob)),
            AttemptAsync(() => second.Get<IFriendshipRequestService>().SendAsync(first.Alice)));
        Assert.Equal(1, outcomes.Count(success => success));
        await using var check = database.CreateContext();
        Assert.Single(await check.FriendshipRequests.Where(r => r.Status == FriendshipRequestStatus.Pending
            && (r.SenderUserId == first.Alice || r.SenderUserId == first.Bob)).ToListAsync());
    }

    [Fact]
    public async Task ConcurrentAcceptanceProducesOneAcceptedRequestAndOneFriendship()
    {
        await using var seed = new TestEnvironment(database);
        await seed.Get<IFriendshipRequestService>().SendAsync(seed.Bob);
        var id = Assert.Single(await seed.Get<IFriendshipRequestService>().GetSentAsync()).Id;
        await using var first = new TestEnvironment(database, seed.Directory, seed.Bob, seed.Clock);
        await using var second = new TestEnvironment(database, seed.Directory, seed.Bob, seed.Clock);
        var outcomes = await Task.WhenAll(
            AttemptAsync(() => first.Get<IFriendshipRequestService>().AcceptAsync(id)),
            AttemptAsync(() => second.Get<IFriendshipRequestService>().AcceptAsync(id)));
        Assert.Equal(1, outcomes.Count(success => success));
        await using var check = database.CreateContext();
        Assert.Equal(FriendshipRequestStatus.Accepted, (await check.FriendshipRequests.FindAsync(id))!.Status);
        Assert.Single(await check.Friendships.Where(f => f.User1Id == seed.Alice && f.User2Id == seed.Bob).ToListAsync());
    }

    [Fact]
    public async Task CanonicalIdentityIdsPreventCaseChangedSelfRequestsAndNormalizeReceivers()
    {
        await using var env = new TestEnvironment(database);
        var service = env.Get<IFriendshipRequestService>();
        await Assert.ThrowsAsync<DomainRuleException>(() => service.SendAsync(env.Alice.ToUpperInvariant()));
        await service.SendAsync(env.Bob.ToUpperInvariant());
        var request = Assert.Single(await env.Context.FriendshipRequests.Where(r => r.SenderUserId == env.Alice).ToListAsync());
        Assert.Equal(env.Bob, request.ReceiverUserId);
    }

    /// <summary>Accepts a safe business conflict as a losing concurrent operation.</summary>
    internal static async Task<bool> AttemptAsync(Func<Task> action)
    {
        try { await action(); return true; }
        catch (DomainRuleException) { return false; }
    }
}
