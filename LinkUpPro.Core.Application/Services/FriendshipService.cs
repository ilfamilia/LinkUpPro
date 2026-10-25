using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Social;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.Core.Domain.Interfaces;

namespace LinkUpPro.Core.Application.Services;

/// <summary>Owns friendship visibility, mutual-friend calculations, and logical removal.</summary>
public sealed class FriendshipService(
    IFriendshipRepository friendships, IPostRepository posts, IReactionRepository reactions,
    IUserDirectoryService users, SocialUserGuard guard, IAtomicOperation atomic, IMapper mapper)
    : IFriendshipService
{
    /// <summary>Checks the current relationship and both account activation states.</summary>
    public async Task<bool> AreFriendsAsync(string firstUserId, string secondUserId)
    {
        var target = await users.GetByIdAsync(secondUserId);
        return target?.IsActive == true && (await GetActiveFriendIdsAsync(firstUserId)).Contains(target.Id);
    }

    /// <summary>Returns only friends whose relationship and account remain active.</summary>
    public async Task<List<string>> GetActiveFriendIdsAsync(string userId)
    {
        var source = await users.GetByIdAsync(userId);
        if (source is null || !source.IsActive) return [];
        userId = source.Id; // Use the canonical Identity value when comparing identifiers in memory.
        var relations = await friendships.GetListAsync(f => f.Status == FriendshipStatus.Active
            && (f.User1Id == userId || f.User2Id == userId));
        var ids = relations.Select(f => f.User1Id == userId ? f.User2Id : f.User1Id).Distinct().ToList();
        return (await users.GetByIdsAsync(ids)).Where(u => u.IsActive).Select(u => u.Id).ToList();
    }

    /// <summary>Lists the caller's active friends in alphabetical order with optional partial searching.</summary>
    public async Task<List<FriendDto>> GetFriendsAsync(string? search = null)
    {
        var current = await guard.GetCurrentAsync();
        var ids = await GetActiveFriendIdsAsync(current.Id);
        var profiles = await users.GetByIdsAsync(ids);
        var term = search?.Trim() ?? "";
        var result = new List<FriendDto>();
        foreach (var user in profiles.Where(u => u.IsActive && (term.Length == 0
            || u.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || u.LastName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || u.UserName.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(u => u.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(u => u.LastName, StringComparer.OrdinalIgnoreCase))
            result.Add(new() { User = user, MutualFriendCount = (await GetMutualFriendsAsync(user.Id)).Count });
        return result;
    }

    /// <summary>Authorizes a friend profile separately from the visibility of a comment author.</summary>
    public async Task<UserSummaryDto> GetFriendProfileAsync(string userId)
    {
        var current = await guard.GetCurrentAsync();
        var target = await guard.GetActiveAsync(userId);
        userId = target.Id;
        if (!await AreFriendsAsync(current.Id, userId))
            throw new DomainRuleException("No posee permisos para visualizar el perfil de este usuario.");
        return target;
    }

    /// <summary>Returns the caller's own friends who are also active friends of the selected user.</summary>
    public async Task<List<UserSummaryDto>> GetMutualFriendsAsync(string otherUserId)
    {
        var current = await guard.GetCurrentAsync();
        otherUserId = (await guard.GetActiveAsync(otherUserId)).Id;
        var first = await GetActiveFriendIdsAsync(current.Id);
        var second = await GetActiveFriendIdsAsync(otherUserId);
        var common = first.Intersect(second).Where(id => id != current.Id && id != otherUserId);
        return (await users.GetByIdsAsync(common)).Where(u => u.IsActive)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
    }

    /// <summary>Logically removes a friendship while preserving requests, interactions, and games.</summary>
    public Task RemoveAsync(string otherUserId) => atomic.ExecuteAsync(async () =>
    {
        var current = await guard.GetCurrentAsync();
        otherUserId = (await guard.GetActiveAsync(otherUserId)).Id;
        var pair = OrderPair(current.Id, otherUserId);
        var friendship = await friendships.GetByIdAsync(pair.First, pair.Second);
        if (friendship is null || friendship.Status != FriendshipStatus.Active)
            throw new DomainRuleException("La amistad seleccionada ya no se encuentra disponible.");
        friendship.Status = FriendshipStatus.Removed;
        return true;
    });

    /// <summary>Reads visible friend posts with combinable filters and inclusive UTC calendar dates.</summary>
    public async Task<List<FriendPostDto>> GetFriendPostsAsync(FriendPostFilterDto filter)
    {
        var current = await guard.GetCurrentAsync();
        if (filter.From?.Date > filter.Until?.Date)
            throw new DomainRuleException("La fecha inicial no puede ser posterior a la fecha final.");
        if (filter.MediaType.HasValue && !Enum.IsDefined(filter.MediaType.Value))
            throw new DomainRuleException("El tipo de contenido no es válido.");
        var ids = await GetActiveFriendIdsAsync(current.Id);
        if (!string.IsNullOrEmpty(filter.FriendId))
        {
            filter.FriendId = (await guard.GetActiveAsync(filter.FriendId)).Id;
            if (!ids.Contains(filter.FriendId))
                throw new DomainRuleException("El usuario seleccionado ya no forma parte de su lista de amigos.");
            ids = [filter.FriendId];
        }
        var term = filter.Search?.Trim().ToUpperInvariant() ?? "";
        var from = filter.From?.Date;
        var until = filter.Until?.Date;
        var entities = await posts.GetListAsync(p => ids.Contains(p.UserId) && !p.IsDeleted
            && p.Privacy == PostPrivacy.FriendsOnly
            && (term == "" || p.Content.ToUpper().Contains(term))
            && (!filter.MediaType.HasValue || p.MediaType == filter.MediaType)
            && (!from.HasValue || p.CreatedAt >= from)
            && (!until.HasValue || p.CreatedAt.Date <= until)
            && (!filter.IsEdited.HasValue || (p.UpdatedAt != null) == filter.IsEdited));
        var authors = (await users.GetByIdsAsync(ids)).ToDictionary(u => u.Id);
        var postIds = entities.Select(p => p.Id).ToList();
        var allReactions = await reactions.GetListAsync(r => postIds.Contains(r.PostId));
        return entities.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).Select(p =>
        {
            var dto = mapper.Map<FriendPostDto>(p);
            dto.Author = authors[p.UserId];
            dto.LikeCount = allReactions.Count(r => r.PostId == p.Id && r.Type == ReactionType.Like);
            dto.DislikeCount = allReactions.Count(r => r.PostId == p.Id && r.Type == ReactionType.Dislike);
            return dto;
        }).ToList();
    }

    /// <summary>Normalizes user identifiers consistently with the binary SQL pair constraint.</summary>
    public static (string First, string Second) OrderPair(string first, string second)
        => string.CompareOrdinal(first, second) < 0 ? (first, second) : (second, first);
}
