using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Dtos.FriendshipRequest;
using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Core.Application.Services
{
    public class FriendshipRequestService : BaseService, IFriendshipRequestService
    {
        private readonly IFriendshipRequestRepository _friendshipRequestRepository;
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserAccountStatusService _userAccountStatusService;
        private readonly ITransactionManager _transactionManager;
        private readonly IAccountServiceForWebApp _accountServiceForWebApp;

        public FriendshipRequestService(
            IFriendshipRequestRepository friendshipRequestRepository,
            IFriendshipRepository friendshipRepository,
            IUserAccountStatusService userAccountStatusService,
            ITransactionManager transactionManager,
            IAccountServiceForWebApp accountServiceForWebApp)
        {
            _friendshipRequestRepository = friendshipRequestRepository;
            _friendshipRepository = friendshipRepository;
            _userAccountStatusService = userAccountStatusService;
            _transactionManager = transactionManager;
            _accountServiceForWebApp = accountServiceForWebApp;
        }

        public async Task<ServiceResult> SendAsync(string senderUserId, string receiverUserId)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(senderUserId))
                {
                    return ServiceResult.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado.");
                }

                if (string.IsNullOrWhiteSpace(receiverUserId))
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "Debe seleccionar un usuario para enviar la solicitud de amistad.");
                }

                if (senderUserId == receiverUserId)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "No puede enviarse una solicitud de amistad a sí mismo.");
                }

                if (!await BothUsersAreActiveAsync(senderUserId, receiverUserId))
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No se puede enviar la solicitud porque uno de los usuarios se encuentra inactivo.");
                }

                (string firstId, string secondId) = OrderUserIds(senderUserId, receiverUserId);
                Friendship? friendship = await _friendshipRepository.GetByIdAsync(firstId, secondId);

                if (friendship?.Status == FriendshipStatus.Active)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "Este usuario ya forma parte de su lista de amigos.");
                }

                FriendshipRequest? pendingRequest = await _friendshipRequestRepository.GetAllQuery()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(request =>
                        request.Status == FriendshipRequestStatus.Pending &&
                        ((request.SenderUserId == senderUserId && request.ReceiverUserId == receiverUserId) ||
                         (request.SenderUserId == receiverUserId && request.ReceiverUserId == senderUserId)));

                if (pendingRequest != null)
                {
                    string message = pendingRequest.SenderUserId == senderUserId
                        ? "Ya envió una solicitud de amistad a este usuario y se encuentra pendiente de respuesta."
                        : "Este usuario ya le envió una solicitud de amistad. Debe aceptarla o rechazarla desde sus solicitudes pendientes.";

                    return ServiceResult.Failure(OperationStatus.Conflict, message);
                }

                FriendshipRequest newRequest = new()
                {
                    Id = 0,
                    SenderUserId = senderUserId,
                    ReceiverUserId = receiverUserId,
                    Status = FriendshipRequestStatus.Pending,
                    RequestedAt = DateTime.UtcNow,
                    RespondedAt = null,
                    CancelledAt = null,
                    IsVisibleToSender = true
                };

                try
                {
                    // El índice único de solicitudes pendientes evita duplicados concurrentes.
                    await _friendshipRequestRepository.AddAsync(newRequest);
                }
                catch (DbUpdateException)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "No se pudo enviar la solicitud. Compruebe si ya existe otra solicitud pendiente.");
                }

                return ServiceResult.Success();
            });
        }

        public async Task<ServiceResult> AcceptAsync(int requestId, string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return ServiceResult.Failure(OperationStatus.Unauthorized,
                    "El usuario no está autenticado.");
            }

            if (requestId <= 0)
            {
                return ServiceResult.Failure(OperationStatus.ValidationError,
                    "El identificador de la solicitud es inválido.");
            }

            bool transactionStarted = false;
            bool committed = false;

            try
            {
                await _transactionManager.BeginTransactionAsync();
                transactionStarted = true;

                FriendshipRequest? request = await _friendshipRequestRepository.GetAllQuery()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == requestId);

                if (request == null)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La solicitud no existe.");
                }

                if (request.ReceiverUserId != currentUserId)
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No posee permisos para realizar esta acción sobre la solicitud.");
                }

                if (request.Status != FriendshipRequestStatus.Pending ||
                    request.SenderUserId == request.ReceiverUserId)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "Esta solicitud ya no se encuentra disponible para ser aceptada.");
                }

                if (!await BothUsersAreActiveAsync(request.SenderUserId, request.ReceiverUserId))
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No se puede aceptar la solicitud porque uno de los usuarios se encuentra inactivo.");
                }

                (string firstId, string secondId) = OrderUserIds(
                    request.SenderUserId, request.ReceiverUserId);

                Friendship? friendship = await _friendshipRepository.GetByIdAsync(firstId, secondId);

                if (friendship?.Status == FriendshipStatus.Active)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "Este usuario ya forma parte de su lista de amigos.");
                }

                DateTime now = DateTime.UtcNow;

                int changed = await _friendshipRequestRepository.GetAllQuery()
                    .Where(item => item.Id == requestId &&
                                   item.ReceiverUserId == currentUserId &&
                                   item.Status == FriendshipRequestStatus.Pending)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.Status, FriendshipRequestStatus.Accepted)
                        .SetProperty(item => item.RespondedAt, now));

                if (changed != 1)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "Esta solicitud ya no se encuentra disponible para ser aceptada.");
                }

                if (friendship == null)
                {
                    Friendship newFriendship = new()
                    {
                        User1Id = firstId,
                        User2Id = secondId,
                        Status = FriendshipStatus.Active
                    };

                    await _friendshipRepository.AddAsync(newFriendship);
                }
                else
                {
                    friendship.Status = FriendshipStatus.Active;
                    Friendship? updated = await _friendshipRepository.UpdateAsync(
                        friendship, firstId, secondId);

                    if (updated == null)
                    {
                        return ServiceResult.Failure(OperationStatus.Failed,
                            "No se pudo reactivar la amistad.");
                    }
                }

                await _transactionManager.CommitAsync();
                committed = true;
                return ServiceResult.Success();
            }
            catch (DbUpdateException)
            {
                return ServiceResult.Failure(OperationStatus.Conflict,
                    "No se pudo aceptar la solicitud debido a un cambio simultáneo de datos.");
            }
            catch (Exception)
            {
                return ServiceResult.Failure(OperationStatus.Failed,
                    "No se pudo aceptar la solicitud de amistad.");
            }
            finally
            {
                if (transactionStarted && !committed)
                {
                    try
                    {
                        await _transactionManager.RollbackAsync();
                    }
                    catch (Exception)
                    {
                        // El administrador puede haber cerrado la transacción
                        // tras un intento fallido de confirmación.
                    }
                }
            }
        }

        public async Task<ServiceResult> RejectAsync(int requestId, string currentUserId)
        {
            return await ExecuteAsync(async () =>
            {
                ServiceResult? invalid = ValidateBasicRequestArguments(requestId, currentUserId);
                if (invalid != null) return invalid;

                FriendshipRequest? request = await _friendshipRequestRepository.GetByIdAsync(requestId);

                if (request == null)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound, "La solicitud no existe.");
                }

                if (request.ReceiverUserId != currentUserId)
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No posee permisos para realizar esta acción sobre la solicitud.");
                }

                if (request.Status != FriendshipRequestStatus.Pending)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "Esta solicitud ya no se encuentra disponible para ser rechazada.");
                }

                if (!await BothUsersAreActiveAsync(request.SenderUserId, request.ReceiverUserId))
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No se puede rechazar la solicitud porque uno de los usuarios se encuentra inactivo.");
                }

                DateTime now = DateTime.UtcNow;

                int changed = await _friendshipRequestRepository.GetAllQuery()
                    .Where(item => item.Id == requestId &&
                                   item.ReceiverUserId == currentUserId &&
                                   item.Status == FriendshipRequestStatus.Pending)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.Status, FriendshipRequestStatus.Rejected)
                        .SetProperty(item => item.RespondedAt, now));

                return changed == 1
                    ? ServiceResult.Success()
                    : ServiceResult.Failure(OperationStatus.Conflict,
                        "Esta solicitud ya no se encuentra disponible para ser rechazada.");
            });
        }

        public async Task<ServiceResult> CancelAsync(int requestId, string currentUserId)
        {
            return await ExecuteAsync(async () =>
            {
                ServiceResult? invalid = ValidateBasicRequestArguments(requestId, currentUserId);
                if (invalid != null) return invalid;

                FriendshipRequest? request = await _friendshipRequestRepository.GetByIdAsync(requestId);

                if (request == null)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound, "La solicitud no existe.");
                }

                if (request.SenderUserId != currentUserId)
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No posee permisos para realizar esta acción sobre la solicitud.");
                }

                if (request.Status != FriendshipRequestStatus.Pending)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "Esta solicitud ya no se encuentra disponible para ser cancelada.");
                }

                if (!await BothUsersAreActiveAsync(request.SenderUserId, request.ReceiverUserId))
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No se puede cancelar la solicitud porque uno de los usuarios se encuentra inactivo.");
                }

                DateTime now = DateTime.UtcNow;

                int changed = await _friendshipRequestRepository.GetAllQuery()
                    .Where(item => item.Id == requestId &&
                                   item.SenderUserId == currentUserId &&
                                   item.Status == FriendshipRequestStatus.Pending)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.Status, FriendshipRequestStatus.Cancelled)
                        .SetProperty(item => item.CancelledAt, now));

                return changed == 1
                    ? ServiceResult.Success()
                    : ServiceResult.Failure(OperationStatus.Conflict,
                        "Esta solicitud ya no se encuentra disponible para ser cancelada.");
            });
        }

        public async Task<ServiceResult> HideFromHistoryAsync(int requestId, string currentUserId)
        {
            return await ExecuteAsync(async () =>
            {
                ServiceResult? invalid = ValidateBasicRequestArguments(requestId, currentUserId);
                if (invalid != null) return invalid;

                FriendshipRequest? request = await _friendshipRequestRepository.GetByIdAsync(requestId);

                if (request == null)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound, "La solicitud no existe.");
                }

                if (request.SenderUserId != currentUserId)
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No posee permisos para realizar esta acción sobre la solicitud.");
                }

                if (request.Status is not (FriendshipRequestStatus.Accepted or FriendshipRequestStatus.Rejected) ||
                    !request.IsVisibleToSender)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "Esta solicitud no se encuentra disponible para eliminarla del historial.");
                }

                if (!await BothUsersAreActiveAsync(request.SenderUserId, request.ReceiverUserId))
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No se puede modificar una solicitud relacionada con cuentas inactivas.");
                }

                int changed = await _friendshipRequestRepository.GetAllQuery()
                    .Where(item => item.Id == requestId &&
                                   item.SenderUserId == currentUserId &&
                                   item.IsVisibleToSender &&
                                   (item.Status == FriendshipRequestStatus.Accepted ||
                                    item.Status == FriendshipRequestStatus.Rejected))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.IsVisibleToSender, false));

                return changed == 1
                    ? ServiceResult.Success()
                    : ServiceResult.Failure(OperationStatus.Conflict,
                        "Esta solicitud ya no se encuentra disponible en el historial.");
            });
        }

        public async Task<ServiceResultWithData<List<FriendshipRequestListDto>>> GetReceivedPendingAsync(string currentUserId)
        {
            return await ExecuteAsync(async () =>
            {
                ServiceResultWithData<List<FriendshipRequestListDto>>? authorization =
                    await ValidateReaderAsync<List<FriendshipRequestListDto>>(currentUserId);
                if (authorization != null) return authorization;

                List<FriendshipRequest> requests = await _friendshipRequestRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(request => request.ReceiverUserId == currentUserId &&
                                      request.Status == FriendshipRequestStatus.Pending)
                    .OrderByDescending(request => request.RequestedAt)
                    .ThenByDescending(request => request.Id)
                    .ToListAsync();

                if (requests.Count == 0)
                    return ServiceResultWithData<List<FriendshipRequestListDto>>.Success([]);

                Dictionary<string, UserDto> activeUsers = await GetActiveUsersByIdAsync();
                string[] otherUserIds = requests.Select(request => request.SenderUserId).Distinct().ToArray();
                Dictionary<string, HashSet<string>> friendships = await GetFriendLookupAsync(
                    currentUserId, otherUserIds, activeUsers);

                HashSet<string> currentFriends = GetFriends(friendships, currentUserId);
                List<FriendshipRequestListDto> results = requests
                    .Where(request => activeUsers.ContainsKey(request.SenderUserId) &&
                                      !currentFriends.Contains(request.SenderUserId))
                    .Select(request => new FriendshipRequestListDto
                    {
                        Id = request.Id,
                        OtherUser = ToFriendshipUserDto(activeUsers[request.SenderUserId]),
                        Status = request.Status,
                        RequestedAt = request.RequestedAt,
                        RespondedAt = request.RespondedAt,
                        MutualFriendsCount = CountMutualFriends(friendships, currentUserId, request.SenderUserId)
                    })
                    .ToList();

                return ServiceResultWithData<List<FriendshipRequestListDto>>.Success(results);
            });
        }

        public async Task<ServiceResultWithData<List<FriendshipRequestListDto>>> GetSentAsync(string currentUserId)
        {
            return await ExecuteAsync(async () =>
            {
                ServiceResultWithData<List<FriendshipRequestListDto>>? authorization =
                    await ValidateReaderAsync<List<FriendshipRequestListDto>>(currentUserId);
                if (authorization != null) return authorization;

                List<FriendshipRequest> requests = await _friendshipRequestRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(request => request.SenderUserId == currentUserId &&
                                      (request.Status == FriendshipRequestStatus.Pending ||
                                       ((request.Status == FriendshipRequestStatus.Accepted ||
                                         request.Status == FriendshipRequestStatus.Rejected) &&
                                        request.IsVisibleToSender)))
                    .OrderByDescending(request => request.RequestedAt)
                    .ThenByDescending(request => request.Id)
                    .ToListAsync();

                if (requests.Count == 0)
                    return ServiceResultWithData<List<FriendshipRequestListDto>>.Success([]);

                Dictionary<string, UserDto> activeUsers = await GetActiveUsersByIdAsync();
                string[] otherUserIds = requests.Select(request => request.ReceiverUserId).Distinct().ToArray();
                Dictionary<string, HashSet<string>> friendships = await GetFriendLookupAsync(
                    currentUserId, otherUserIds, activeUsers);

                List<FriendshipRequestListDto> results = requests
                    .Where(request => activeUsers.ContainsKey(request.ReceiverUserId))
                    .Select(request => new FriendshipRequestListDto
                    {
                        Id = request.Id,
                        OtherUser = ToFriendshipUserDto(activeUsers[request.ReceiverUserId]),
                        Status = request.Status,
                        RequestedAt = request.RequestedAt,
                        RespondedAt = request.RespondedAt,
                        MutualFriendsCount = CountMutualFriends(friendships, currentUserId, request.ReceiverUserId)
                    })
                    .ToList();

                return ServiceResultWithData<List<FriendshipRequestListDto>>.Success(results);
            });
        }

        public async Task<ServiceResultWithData<List<AvailableFriendshipUserDto>>> GetAvailableUsersAsync(
            string currentUserId, string? userNameFilter = null)
        {
            return await ExecuteAsync(async () =>
            {
                ServiceResultWithData<List<AvailableFriendshipUserDto>>? authorization =
                    await ValidateReaderAsync<List<AvailableFriendshipUserDto>>(currentUserId);
                if (authorization != null) return authorization;

                Dictionary<string, UserDto> activeUsers = await GetActiveUsersByIdAsync();
                string[] otherUserIds = activeUsers.Keys
                    .Where(userId => userId != currentUserId)
                    .ToArray();

                Dictionary<string, HashSet<string>> friendships = await GetFriendLookupAsync(
                    currentUserId, otherUserIds, activeUsers);
                HashSet<string> currentFriends = GetFriends(friendships, currentUserId);

                List<FriendshipRequest> pendingRequests = await _friendshipRequestRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(request => request.Status == FriendshipRequestStatus.Pending &&
                                      (request.SenderUserId == currentUserId ||
                                       request.ReceiverUserId == currentUserId))
                    .ToListAsync();

                HashSet<string> pendingUserIds = pendingRequests
                    .Select(request => request.SenderUserId == currentUserId
                        ? request.ReceiverUserId : request.SenderUserId)
                    .ToHashSet(StringComparer.Ordinal);

                string filter = userNameFilter?.Trim() ?? string.Empty;
                List<AvailableFriendshipUserDto> results = activeUsers.Values
                    .Where(user => user.Id != currentUserId &&
                                   !currentFriends.Contains(user.Id) &&
                                   !pendingUserIds.Contains(user.Id) &&
                                   (filter.Length == 0 ||
                                    user.UserName.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(user => user.UserName, StringComparer.OrdinalIgnoreCase)
                    .Select(user => new AvailableFriendshipUserDto
                    {
                        User = ToFriendshipUserDto(user),
                        MutualFriendsCount = CountMutualFriends(friendships, currentUserId, user.Id)
                    })
                    .ToList();

                return ServiceResultWithData<List<AvailableFriendshipUserDto>>.Success(results);
            });
        }

        public async Task<ServiceResultWithData<List<FriendshipUserDto>>> GetMutualFriendsAsync(
            string currentUserId, string otherUserId)
        {
            return await ExecuteAsync(async () =>
            {
                ServiceResultWithData<List<FriendshipUserDto>>? authorization =
                    await ValidateReaderAsync<List<FriendshipUserDto>>(currentUserId);
                if (authorization != null) return authorization;

                if (string.IsNullOrWhiteSpace(otherUserId) || currentUserId == otherUserId)
                {
                    return ServiceResultWithData<List<FriendshipUserDto>>.Failure(
                        OperationStatus.ValidationError, "Debe seleccionar otro usuario válido.");
                }

                Dictionary<string, UserDto> activeUsers = await GetActiveUsersByIdAsync();
                if (!activeUsers.ContainsKey(otherUserId))
                {
                    return ServiceResultWithData<List<FriendshipUserDto>>.Failure(
                        OperationStatus.NotFound, "El usuario no existe o se encuentra inactivo.");
                }

                Dictionary<string, HashSet<string>> friendships = await GetFriendLookupAsync(
                    currentUserId, [otherUserId], activeUsers);

                HashSet<string> currentFriends = GetFriends(friendships, currentUserId);
                HashSet<string> otherFriends = GetFriends(friendships, otherUserId);

                List<FriendshipUserDto> results = currentFriends
                    .Where(userId => userId != currentUserId && userId != otherUserId &&
                                     otherFriends.Contains(userId) && activeUsers.ContainsKey(userId))
                    .Select(userId => ToFriendshipUserDto(activeUsers[userId]))
                    .OrderBy(user => user.UserName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return ServiceResultWithData<List<FriendshipUserDto>>.Success(results);
            });
        }

        public async Task<ServiceResultWithData<int>> GetReceivedPendingCountAsync(string currentUserId)
        {
            return await ExecuteAsync(async () =>
            {
                ServiceResultWithData<int>? authorization = await ValidateReaderAsync<int>(currentUserId);
                if (authorization != null) return authorization;

                List<string> senders = await _friendshipRequestRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(request => request.ReceiverUserId == currentUserId &&
                                      request.Status == FriendshipRequestStatus.Pending)
                    .Select(request => request.SenderUserId)
                    .ToListAsync();

                if (senders.Count == 0) return ServiceResultWithData<int>.Success(0);

                HashSet<string> activeSenders = await _userAccountStatusService.GetActiveUserIdsAsync(senders);
                HashSet<string> activeFriendIds = (await _friendshipRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(friendship => friendship.Status == FriendshipStatus.Active &&
                                         (friendship.User1Id == currentUserId ||
                                          friendship.User2Id == currentUserId))
                    .ToListAsync())
                    .Select(friendship => friendship.User1Id == currentUserId
                        ? friendship.User2Id : friendship.User1Id)
                    .ToHashSet(StringComparer.Ordinal);

                int count = senders.Count(senderId => activeSenders.Contains(senderId) &&
                                                      !activeFriendIds.Contains(senderId));

                return ServiceResultWithData<int>.Success(count);
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

        private async Task<Dictionary<string, UserDto>> GetActiveUsersByIdAsync()
        {
            List<UserDto> users = await _accountServiceForWebApp.GetAllUserAsync(true);
            return users.ToDictionary(user => user.Id, StringComparer.Ordinal);
        }

        private async Task<Dictionary<string, HashSet<string>>> GetFriendLookupAsync(
            string currentUserId, string[] otherUserIds, Dictionary<string, UserDto> activeUsers)
        {
            List<Friendship> pairs = await _friendshipRepository.GetAllQuery()
                .AsNoTracking()
                .Where(friendship => friendship.Status == FriendshipStatus.Active &&
                                     (friendship.User1Id == currentUserId ||
                                      friendship.User2Id == currentUserId ||
                                      otherUserIds.Contains(friendship.User1Id) ||
                                      otherUserIds.Contains(friendship.User2Id)))
                .ToListAsync();

            Dictionary<string, HashSet<string>> lookup = new(StringComparer.Ordinal);

            foreach (Friendship pair in pairs)
            {
                if (!activeUsers.ContainsKey(pair.User1Id) || !activeUsers.ContainsKey(pair.User2Id))
                    continue;

                if (!lookup.TryGetValue(pair.User1Id, out HashSet<string>? firstFriends))
                    lookup[pair.User1Id] = firstFriends = new HashSet<string>(StringComparer.Ordinal);
                firstFriends.Add(pair.User2Id);

                if (!lookup.TryGetValue(pair.User2Id, out HashSet<string>? secondFriends))
                    lookup[pair.User2Id] = secondFriends = new HashSet<string>(StringComparer.Ordinal);
                secondFriends.Add(pair.User1Id);
            }

            return lookup;
        }

        private static HashSet<string> GetFriends(
            Dictionary<string, HashSet<string>> lookup, string userId)
        {
            return lookup.TryGetValue(userId, out HashSet<string>? friends)
                ? friends
                : new HashSet<string>(StringComparer.Ordinal);
        }

        private static int CountMutualFriends(
            Dictionary<string, HashSet<string>> lookup, string firstUserId, string secondUserId)
        {
            HashSet<string> firstFriends = GetFriends(lookup, firstUserId);
            HashSet<string> secondFriends = GetFriends(lookup, secondUserId);

            if (firstFriends.Count > secondFriends.Count)
                (firstFriends, secondFriends) = (secondFriends, firstFriends);

            return firstFriends.Count(userId => userId != firstUserId && userId != secondUserId &&
                                                secondFriends.Contains(userId));
        }

        private static FriendshipUserDto ToFriendshipUserDto(UserDto user)
        {
            return new FriendshipUserDto
            {
                Id = user.Id,
                Name = user.Name,
                LastName = user.LastName,
                UserName = user.UserName,
                ProfileImage = user.ProfileImage
            };
        }

        private async Task<bool> BothUsersAreActiveAsync(string firstUserId, string secondUserId)
        {
            HashSet<string> activeUserIds = await _userAccountStatusService.GetActiveUserIdsAsync(
                [firstUserId, secondUserId]);

            return activeUserIds.Contains(firstUserId) && activeUserIds.Contains(secondUserId);
        }

        private static (string firstId, string secondId) OrderUserIds(string userId1, string userId2)
        {
            return string.CompareOrdinal(userId1, userId2) < 0
                ? (userId1, userId2)
                : (userId2, userId1);
        }

        private static ServiceResult? ValidateBasicRequestArguments(int requestId, string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return ServiceResult.Failure(OperationStatus.Unauthorized,
                    "El usuario no está autenticado.");
            }

            if (requestId <= 0)
            {
                return ServiceResult.Failure(OperationStatus.ValidationError,
                    "El identificador de la solicitud es inválido.");
            }

            return null;
        }
    }
}