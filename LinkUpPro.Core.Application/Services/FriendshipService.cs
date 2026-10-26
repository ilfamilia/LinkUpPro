using LinkUpPro.Core.Application.Dtos.Friendship;
using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Core.Application.Services
{
    public class FriendshipService : BaseService, IFriendshipService
    {
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IPostRepository _postRepository;
        private readonly IUserAccountStatusService _userAccountStatusService;
        private readonly IAccountServiceForWebApp _accountServiceForWebApp;

        public FriendshipService(
            IFriendshipRepository friendshipRepository,
            IPostRepository postRepository,
            IUserAccountStatusService userAccountStatusService,
            IAccountServiceForWebApp accountServiceForWebApp)
        {
            _friendshipRepository = friendshipRepository;
            _postRepository = postRepository;
            _userAccountStatusService = userAccountStatusService;
            _accountServiceForWebApp = accountServiceForWebApp;
        }

        public async Task<ServiceResultWithData<List<FriendDto>>> GetFriendsAsync(string currentUserId, string? searchText = null)
        {
            return await ExecuteAsync<List<FriendDto>>(async () =>
            {
                ServiceResultWithData<List<FriendDto>>? failure =
                    await ValidateReaderAsync<List<FriendDto>>(currentUserId);
                if (failure != null) return failure;

                FriendNetwork network = await GetFriendNetworkAsync(currentUserId);
                IEnumerable<FriendDto> friends = network.FriendIds
                    .Select(id => CreateFriendDto(
                        network.ActiveUsers[id],
                        GetMutualCount(network, currentUserId, id)));

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    string term = searchText.Trim();
                    friends = friends.Where(friend =>
                        friend.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        friend.LastName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        friend.UserName.Contains(term, StringComparison.OrdinalIgnoreCase));
                }

                return ServiceResultWithData<List<FriendDto>>.Success(SortFriends(friends));
            });
        }

        public async Task<ServiceResultWithData<FriendDto>> GetFriendAsync(string currentUserId, string friendUserId)
        {
            return await ExecuteAsync<FriendDto>(async () =>
            {
                ServiceResultWithData<FriendDto>? failure =
                    await ValidateReaderAsync<FriendDto>(currentUserId);
                if (failure != null) return failure;

                if (string.IsNullOrWhiteSpace(friendUserId) || currentUserId == friendUserId)
                {
                    return ServiceResultWithData<FriendDto>.Failure(
                        OperationStatus.ValidationError, "Debe seleccionar un amigo válido.");
                }

                FriendNetwork network = await GetFriendNetworkAsync(currentUserId);
                if (!network.FriendIds.Contains(friendUserId))
                {
                    return ServiceResultWithData<FriendDto>.Failure(
                        OperationStatus.Forbidden,
                        "No posee permisos para visualizar el perfil de este usuario.");
                }

                FriendDto friend = CreateFriendDto(network.ActiveUsers[friendUserId],
                    GetMutualCount(network, currentUserId, friendUserId));
                return ServiceResultWithData<FriendDto>.Success(friend);
            });
        }

        public async Task<ServiceResultWithData<List<FriendDto>>> GetMutualFriendsAsync(string currentUserId, string friendUserId)
        {
            return await ExecuteAsync<List<FriendDto>>(async () =>
            {
                ServiceResultWithData<List<FriendDto>>? failure =
                    await ValidateReaderAsync<List<FriendDto>>(currentUserId);
                if (failure != null) return failure;

                if (string.IsNullOrWhiteSpace(friendUserId) || currentUserId == friendUserId)
                {
                    return ServiceResultWithData<List<FriendDto>>.Failure(OperationStatus.ValidationError, "Debe seleccionar un amigo válido.");
                }

                FriendNetwork network = await GetFriendNetworkAsync(currentUserId);
                if (!network.FriendIds.Contains(friendUserId))
                {
                    return ServiceResultWithData<List<FriendDto>>.Failure(OperationStatus.Forbidden,
                        "No posee permisos para consultar los amigos en común de este usuario.");
                }

                HashSet<string> friendConnections = GetConnections(network, friendUserId);
                List<FriendDto> mutualFriends = SortFriends(network.FriendIds
                    .Where(id => id != currentUserId && id != friendUserId &&
                                 friendConnections.Contains(id))
                    .Select(id => CreateFriendDto(network.ActiveUsers[id],
                        GetMutualCount(network, currentUserId, id))));

                return ServiceResultWithData<List<FriendDto>>.Success(mutualFriends);
            });
        }

        public async Task<ServiceResultWithData<FriendshipSummaryDto>> GetSummaryAsync(string currentUserId)
        {
            return await ExecuteAsync<FriendshipSummaryDto>(async () =>
            {
                ServiceResultWithData<FriendshipSummaryDto>? failure =
                    await ValidateReaderAsync<FriendshipSummaryDto>(currentUserId);
                if (failure != null) return failure;

                FriendNetwork network = await GetFriendNetworkAsync(currentUserId);
                int availablePosts = 0;

                if (network.FriendIds.Count > 0)
                {
                    string[] friendIds = network.FriendIds.ToArray();
                    availablePosts = await _postRepository.GetAllQuery()
                        .AsNoTracking()
                        .CountAsync(post => !post.IsDeleted &&
                            post.Privacy == PostPrivacy.FriendsOnly &&
                            friendIds.Contains(post.UserId));
                }

                return ServiceResultWithData<FriendshipSummaryDto>.Success(
                    new FriendshipSummaryDto
                    {
                        TotalFriends = network.FriendIds.Count,
                        AvailablePosts = availablePosts
                    });
            });
        }

        public async Task<ServiceResult> RemoveAsync(string currentUserId, string friendUserId)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(currentUserId))
                {
                    return ServiceResult.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado.");
                }

                if (string.IsNullOrWhiteSpace(friendUserId) || currentUserId == friendUserId)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "Debe seleccionar un amigo válido.");
                }

                HashSet<string> activeIds = await _userAccountStatusService.GetActiveUserIdsAsync(
                    [currentUserId, friendUserId]);
                if (!activeIds.Contains(currentUserId) || !activeIds.Contains(friendUserId))
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No se puede eliminar la amistad porque una de las cuentas está inactiva.");
                }

                int changed = await _friendshipRepository.GetAllQuery()
                    .Where(friendship => friendship.Status == FriendshipStatus.Active &&
                        ((friendship.User1Id == currentUserId && friendship.User2Id == friendUserId) ||
                         (friendship.User2Id == currentUserId && friendship.User1Id == friendUserId)))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(friendship => friendship.Status, FriendshipStatus.Removed));

                if (changed != 1)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La amistad seleccionada ya no se encuentra disponible.");
                }

                return ServiceResult.Success();
            });
        }

        private async Task<ServiceResultWithData<T>?> ValidateReaderAsync<T>(string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return ServiceResultWithData<T>.Failure(OperationStatus.Unauthorized,
                    "El usuario no está autenticado.");
            }

            if (!await _userAccountStatusService.IsUserActiveAsync(currentUserId))
            {
                return ServiceResultWithData<T>.Failure(OperationStatus.Forbidden,
                    "La cuenta del usuario se encuentra inactiva.");
            }

            return null;
        }

        private async Task<FriendNetwork> GetFriendNetworkAsync(string currentUserId)
        {
            Dictionary<string, UserDto> activeUsers = (await _accountServiceForWebApp
                .GetAllUserAsync(true))
                .ToDictionary(user => user.Id, StringComparer.Ordinal);

            List<Friendship> currentRelationships = await _friendshipRepository.GetAllQuery()
                .AsNoTracking()
                .Where(friendship => friendship.Status == FriendshipStatus.Active &&
                    (friendship.User1Id == currentUserId || friendship.User2Id == currentUserId))
                .ToListAsync();

            HashSet<string> friendIds = currentRelationships
                .Select(friendship => friendship.User1Id == currentUserId
                    ? friendship.User2Id : friendship.User1Id)
                .Where(activeUsers.ContainsKey)
                .ToHashSet(StringComparer.Ordinal);

            Dictionary<string, HashSet<string>> connections = new(StringComparer.Ordinal);
            if (friendIds.Count > 0)
            {
                string[] candidates = friendIds.Append(currentUserId).ToArray();
                List<Friendship> related = await _friendshipRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(friendship => friendship.Status == FriendshipStatus.Active &&
                        (candidates.Contains(friendship.User1Id) ||
                         candidates.Contains(friendship.User2Id)))
                    .ToListAsync();

                foreach (Friendship friendship in related)
                {
                    if (!activeUsers.ContainsKey(friendship.User1Id) ||
                        !activeUsers.ContainsKey(friendship.User2Id)) continue;

                    if (!connections.TryGetValue(friendship.User1Id, out HashSet<string>? first))
                        connections[friendship.User1Id] = first = new HashSet<string>(StringComparer.Ordinal);
                    first.Add(friendship.User2Id);

                    if (!connections.TryGetValue(friendship.User2Id, out HashSet<string>? second))
                        connections[friendship.User2Id] = second = new HashSet<string>(StringComparer.Ordinal);
                    second.Add(friendship.User1Id);
                }
            }

            return new FriendNetwork(activeUsers, friendIds, connections);
        }

        private static HashSet<string> GetConnections(FriendNetwork network, string userId)
        {
            return network.Connections.TryGetValue(userId, out HashSet<string>? friends)
                ? friends : new HashSet<string>(StringComparer.Ordinal);
        }

        private static int GetMutualCount(FriendNetwork network, string currentUserId, string friendUserId)
        {
            HashSet<string> otherFriends = GetConnections(network, friendUserId);
            return network.FriendIds.Count(id => id != currentUserId &&
                                                 id != friendUserId &&
                                                 otherFriends.Contains(id));
        }

        private static FriendDto CreateFriendDto(UserDto user, int mutualCount)
        {
            return new FriendDto
            {
                Id = user.Id,
                Name = user.Name,
                LastName = user.LastName,
                UserName = user.UserName,
                ProfileImage = user.ProfileImage,
                MutualFriendsCount = mutualCount
            };
        }

        private static List<FriendDto> SortFriends(IEnumerable<FriendDto> friends)
        {
            return friends.OrderBy(friend => friend.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(friend => friend.LastName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(friend => friend.UserName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private sealed record FriendNetwork(
            Dictionary<string, UserDto> ActiveUsers,
            HashSet<string> FriendIds,
            Dictionary<string, HashSet<string>> Connections);
    }
}
