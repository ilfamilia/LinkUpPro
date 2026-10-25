using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Social;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.Core.Domain.Interfaces;

namespace LinkUpPro.Core.Application.Services;

/// <summary>Coordinates request transitions and canonical friendships within atomic database operations.</summary>
public sealed class FriendshipRequestService(
    IFriendshipRequestRepository requests, IFriendshipRepository friendships, IFriendshipService friends,
    IUserDirectoryService users, SocialUserGuard guard, IAtomicOperation atomic, TimeProvider clock, IMapper mapper)
    : IFriendshipRequestService
{
    /// <summary>Lists pending received requests from active users who are not already friends.</summary>
    public async Task<List<FriendshipRequestDto>> GetIncomingAsync()
    {
        var current = await guard.GetCurrentAsync();
        var list = await requests.GetListAsync(r => r.ReceiverUserId == current.Id && r.Status == FriendshipRequestStatus.Pending);
        return await ProjectAsync(list, current.Id, true);
    }

    /// <summary>Lists visible sent requests while retaining cancelled and hidden history in storage.</summary>
    public async Task<List<FriendshipRequestDto>> GetSentAsync()
    {
        var current = await guard.GetCurrentAsync();
        return await ProjectAsync(await requests.GetListAsync(r => r.SenderUserId == current.Id
            && r.Status != FriendshipRequestStatus.Cancelled && r.IsVisibleToSender), current.Id, false);
    }

    /// <summary>Filters available accounts by both-direction pending requests and current friendships.</summary>
    public async Task<List<FriendDto>> GetEligibleUsersAsync(string? search = null)
    {
        var current = await guard.GetCurrentAsync();
        var activeFriends = await friends.GetActiveFriendIdsAsync(current.Id);
        var pending = await requests.GetListAsync(r => r.Status == FriendshipRequestStatus.Pending
            && (r.SenderUserId == current.Id || r.ReceiverUserId == current.Id));
        var excluded = pending.Select(r => r.SenderUserId == current.Id ? r.ReceiverUserId : r.SenderUserId)
            .Concat(activeFriends).Append(current.Id).ToHashSet();
        var result = new List<FriendDto>();
        foreach (var user in (await users.SearchActiveAsync(search)).Where(u => !excluded.Contains(u.Id)))
            result.Add(new() { User = user, MutualFriendCount = (await friends.GetMutualFriendsAsync(user.Id)).Count });
        return result.OrderBy(u => u.User.UserName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Counts only received pending requests that are currently actionable.</summary>
    public async Task<int> GetPendingCountAsync() => (await GetIncomingAsync()).Count;

    /// <summary>Rechecks eligibility immediately before creating an independent pending request.</summary>
    public Task SendAsync(string receiverId) => atomic.ExecuteAsync(async () =>
    {
        var current = await guard.GetCurrentAsync();
        receiverId = (await guard.GetActiveAsync(receiverId)).Id;
        if (receiverId == current.Id)
            throw new DomainRuleException("No puede enviarse una solicitud a sí mismo.");
        if (await friends.AreFriendsAsync(current.Id, receiverId))
            throw new DomainRuleException("Este usuario ya forma parte de su lista de amigos.");
        var pending = await requests.GetListAsync(r => r.Status == FriendshipRequestStatus.Pending
            && ((r.SenderUserId == current.Id && r.ReceiverUserId == receiverId)
                || (r.SenderUserId == receiverId && r.ReceiverUserId == current.Id)));
        if (pending.Count > 0)
            throw new DomainRuleException(pending[0].SenderUserId == current.Id
                ? "Ya envió una solicitud de amistad a este usuario y se encuentra pendiente de respuesta."
                : "Este usuario ya le envió una solicitud de amistad. Debe aceptarla o rechazarla.");
        await requests.AddAsync(new() { Id = 0, SenderUserId = current.Id, ReceiverUserId = receiverId,
            Status = FriendshipRequestStatus.Pending, RequestedAt = clock.GetUtcNow().UtcDateTime, IsVisibleToSender = true });
        return true;
    });

    /// <summary>Accepts a pending received request and creates or reactivates exactly one friendship.</summary>
    public Task AcceptAsync(int requestId) => RespondAsync(requestId, true);

    /// <summary>Rejects a pending received request without creating or changing a friendship.</summary>
    public Task RejectAsync(int requestId) => RespondAsync(requestId, false);

    /// <summary>Allows only the sender to cancel a request that still awaits a response.</summary>
    public Task CancelAsync(int requestId) => atomic.ExecuteAsync(async () =>
    {
        var request = await GetOwnedAsync(requestId, false);
        EnsurePending(request);
        request.Status = FriendshipRequestStatus.Cancelled;
        request.CancelledAt = clock.GetUtcNow().UtcDateTime;
        return true;
    });

    /// <summary>Hides accepted or rejected sent history without changing its state or friendship.</summary>
    public Task HideAsync(int requestId) => atomic.ExecuteAsync(async () =>
    {
        var request = await GetOwnedAsync(requestId, false);
        if (!request.IsVisibleToSender || request.Status is not (FriendshipRequestStatus.Accepted or FriendshipRequestStatus.Rejected))
            throw new DomainRuleException("Esta solicitud no puede eliminarse del historial.");
        request.IsVisibleToSender = false;
        return true;
    });

    /// <summary>Performs the receiver's response and its related friendship change as a single unit.</summary>
    private Task RespondAsync(int requestId, bool accepted) => atomic.ExecuteAsync(async () =>
    {
        var request = await GetOwnedAsync(requestId, true);
        EnsurePending(request);
        var pair = FriendshipService.OrderPair(request.SenderUserId, request.ReceiverUserId);
        var relation = await friendships.GetByIdAsync(pair.First, pair.Second);
        if (relation?.Status == FriendshipStatus.Active)
            throw new DomainRuleException("Esta amistad ya se encuentra activa.");
        request.Status = accepted ? FriendshipRequestStatus.Accepted : FriendshipRequestStatus.Rejected;
        request.RespondedAt = clock.GetUtcNow().UtcDateTime;
        if (accepted)
        {
            if (relation is null)
                await friendships.AddAsync(new() { User1Id = pair.First, User2Id = pair.Second, Status = FriendshipStatus.Active });
            else relation.Status = FriendshipStatus.Active;
        }
        return true;
    });

    /// <summary>Validates ownership and both accounts before exposing a request to a transition.</summary>
    private async Task<FriendshipRequest> GetOwnedAsync(int id, bool receiver)
    {
        var current = await guard.GetCurrentAsync();
        var request = await requests.GetByIdAsync(id);
        if (request is null || (receiver ? request.ReceiverUserId : request.SenderUserId) != current.Id)
            throw new DomainRuleException("No posee permisos para realizar esta acción sobre la solicitud.");
        await guard.GetActiveAsync(request.SenderUserId);
        await guard.GetActiveAsync(request.ReceiverUserId);
        return request;
    }

    /// <summary>Prevents repeated responses or cancellation after a request was already handled.</summary>
    private static void EnsurePending(FriendshipRequest request)
    {
        if (request.Status != FriendshipRequestStatus.Pending)
            throw new DomainRuleException("Esta solicitud ya no se encuentra disponible.");
    }

    /// <summary>Combines request data with current public profiles and mutual-friend counts.</summary>
    private async Task<List<FriendshipRequestDto>> ProjectAsync(List<FriendshipRequest> list, string currentId, bool incoming)
    {
        var ids = list.Select(r => incoming ? r.SenderUserId : r.ReceiverUserId).Distinct();
        var profiles = (await users.GetByIdsAsync(ids)).Where(u => u.IsActive).ToDictionary(u => u.Id);
        var result = new List<FriendshipRequestDto>();
        foreach (var request in list.OrderByDescending(r => r.RequestedAt).ThenByDescending(r => r.Id))
        {
            var otherId = incoming ? request.SenderUserId : request.ReceiverUserId;
            if (!profiles.TryGetValue(otherId, out var other)) continue;
            if (incoming && await friends.AreFriendsAsync(currentId, otherId)) continue;
            var dto = mapper.Map<FriendshipRequestDto>(request);
            dto.OtherUser = other;
            dto.MutualFriendCount = (await friends.GetMutualFriendsAsync(otherId)).Count;
            result.Add(dto);
        }
        return result;
    }
}
