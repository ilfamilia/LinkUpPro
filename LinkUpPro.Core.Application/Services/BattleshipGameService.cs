using LinkUpPro.Core.Application.Dtos.Battleship;
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
    public class BattleshipGameService : BaseService, IBattleshipGameService
    {
        private readonly IGenericRepository<BattleshipGame> _gameRepository;
        private readonly IGenericRepository<BattleshipAttack> _attackRepository;
        private readonly IFriendshipService _friendshipService;
        private readonly IUserAccountStatusService _userAccountStatusService;
        private readonly IAccountServiceForWebApp _accountService;

        public BattleshipGameService(
            IGenericRepository<BattleshipGame> gameRepository,
            IGenericRepository<BattleshipAttack> attackRepository,
            IFriendshipService friendshipService,
            IUserAccountStatusService userAccountStatusService,
            IAccountServiceForWebApp accountService)
        {
            _gameRepository = gameRepository;
            _attackRepository = attackRepository;
            _friendshipService = friendshipService;
            _userAccountStatusService = userAccountStatusService;
            _accountService = accountService;
        }

        public async Task<ServiceResultWithData<List<FriendDto>>> GetAvailableOpponentsAsync(
            string currentUserId, string? searchText = null)
        {
            return await ExecuteAsync<List<FriendDto>>(async () =>
            {
                ServiceResultWithData<List<FriendDto>>? failure =
                    await ValidateCurrentUserAsync<List<FriendDto>>(currentUserId);

                if (failure != null) return failure;

                ServiceResult expiry = await ExpireOverdueGamesAsync(currentUserId);
                if (!expiry.IsSuccess)
                    return ServiceResultWithData<List<FriendDto>>.Failure(expiry.Status, expiry.Errors.ToArray());

                ServiceResultWithData<List<FriendDto>> friendsResult =
                    await _friendshipService.GetFriendsAsync(currentUserId, searchText);

                if (!friendsResult.IsSuccess)
                {
                    return ServiceResultWithData<List<FriendDto>>.Failure(
                        friendsResult.Status, friendsResult.Errors.ToArray());
                }

                string[] unavailableIds = await _gameRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(game => game.Status != BattleshipGameStatus.Finished &&
                        (game.CreatorUserId == currentUserId ||
                         game.OpponentUserId == currentUserId))
                    .Select(game => game.CreatorUserId == currentUserId
                        ? game.OpponentUserId : game.CreatorUserId)
                    .ToArrayAsync();

                HashSet<string> unavailableSet = unavailableIds.ToHashSet();

                List<FriendDto> available = (friendsResult.Data ?? [])
                    .Where(friend => !unavailableSet.Contains(friend.Id))
                    .ToList();

                return ServiceResultWithData<List<FriendDto>>.Success(available);
            });
        }

        public async Task<ServiceResultWithData<int>> CreateAsync(
            string currentUserId, string opponentUserId)
        {
            return await ExecuteAsync<int>(async () =>
            {
                ServiceResultWithData<int>? failure =
                    await ValidateCurrentUserAsync<int>(currentUserId);

                if (failure != null) return failure;

                ServiceResult expiry = await ExpireOverdueGamesAsync(currentUserId);
                if (!expiry.IsSuccess)
                    return ServiceResultWithData<int>.Failure(expiry.Status, expiry.Errors.ToArray());

                if (string.IsNullOrWhiteSpace(opponentUserId) ||
                    opponentUserId == currentUserId)
                {
                    return ServiceResultWithData<int>.Failure(
                        OperationStatus.ValidationError,
                        "Debe seleccionar un amigo diferente de usted.");
                }

                ServiceResultWithData<FriendDto> friendResult =
                    await _friendshipService.GetFriendAsync(currentUserId, opponentUserId);

                if (!friendResult.IsSuccess)
                {
                    return ServiceResultWithData<int>.Failure(
                        friendResult.Status, friendResult.Errors.ToArray());
                }

                if (await HasUnfinishedGameAsync(currentUserId, opponentUserId))
                {
                    return ServiceResultWithData<int>.Failure(
                        OperationStatus.Conflict,
                        "Ya existe una partida sin finalizar entre ambos usuarios.");
                }

                BattleshipGame game = new()
                {
                    Id = 0,
                    CreatorUserId = currentUserId,
                    OpponentUserId = opponentUserId,
                    Status = BattleshipGameStatus.SettingUp,
                    CreatedAt = DateTime.UtcNow,
                    CurrentTurnUserId = null,
                    WinnerUserId = null,
                    EndReason = null,
                    BattleStartedAt = null,
                    TurnStartedAt = null,
                    FinishedAt = null
                };

                try
                {
                    BattleshipGame created = await _gameRepository.AddAsync(game);
                    return ServiceResultWithData<int>.Success(created.Id);
                }
                catch (DbUpdateException)
                {
                    if (await HasUnfinishedGameAsync(currentUserId, opponentUserId))
                    {
                        return ServiceResultWithData<int>.Failure(
                            OperationStatus.Conflict,
                            "Ya existe una partida sin finalizar entre ambos usuarios.");
                    }

                    throw;
                }
            });
        }

        public async Task<ServiceResultWithData<BattleshipGameListDto>> GetMyGamesAsync(
            string currentUserId)
        {
            return await ExecuteAsync<BattleshipGameListDto>(async () =>
            {
                ServiceResultWithData<BattleshipGameListDto>? failure =
                    await ValidateCurrentUserAsync<BattleshipGameListDto>(currentUserId);

                if (failure != null) return failure;

                ServiceResult expiry = await ExpireOverdueGamesAsync(currentUserId);
                if (!expiry.IsSuccess)
                    return ServiceResultWithData<BattleshipGameListDto>.Failure(expiry.Status, expiry.Errors.ToArray());

                List<BattleshipGame> games = await _gameRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(game => game.CreatorUserId == currentUserId ||
                                   game.OpponentUserId == currentUserId)
                    .OrderByDescending(game => game.CreatedAt)
                    .ToListAsync();

                Dictionary<string, UserDto> users = (await _accountService
                    .GetAllUserAsync(null))
                    .ToDictionary(user => user.Id);

                BattleshipGameListDto data = new();

                foreach (BattleshipGame game in games)
                {
                    BattleshipGameDto dto = MapGame(game, currentUserId, users);

                    if (game.Status == BattleshipGameStatus.Finished)
                        data.FinishedGames.Add(dto);
                    else
                        data.ActiveGames.Add(dto);
                }

                data.TotalGames = data.FinishedGames.Count;
                data.WonGames = data.FinishedGames.Count(g => g.DidIWin == true);
                data.LostGames = data.FinishedGames.Count(g => g.DidIWin == false);

                return ServiceResultWithData<BattleshipGameListDto>.Success(data);
            });
        }

        public async Task<ServiceResultWithData<BattleshipGameDto>> GetGameAsync(
            int gameId, string currentUserId)
        {
            return await ExecuteAsync<BattleshipGameDto>(async () =>
            {
                ServiceResultWithData<BattleshipGameDto>? failure =
                    await ValidateCurrentUserAsync<BattleshipGameDto>(currentUserId);

                if (failure != null) return failure;

                ServiceResult expiry = await ExpireOverdueGamesAsync(currentUserId);
                if (!expiry.IsSuccess)
                    return ServiceResultWithData<BattleshipGameDto>.Failure(expiry.Status, expiry.Errors.ToArray());

                if (gameId <= 0)
                {
                    return ServiceResultWithData<BattleshipGameDto>.Failure(
                        OperationStatus.ValidationError,
                        "El identificador de la partida es inválido.");
                }

                BattleshipGame? game = await _gameRepository.GetAllQuery()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == gameId);

                if (game == null)
                {
                    return ServiceResultWithData<BattleshipGameDto>.Failure(
                        OperationStatus.NotFound, "La partida no fue encontrada.");
                }

                if (game.CreatorUserId != currentUserId &&
                    game.OpponentUserId != currentUserId)
                {
                    return ServiceResultWithData<BattleshipGameDto>.Failure(
                        OperationStatus.Forbidden,
                        "No posee permisos para consultar esta partida.");
                }

                string opponentId = game.CreatorUserId == currentUserId
                    ? game.OpponentUserId : game.CreatorUserId;

                UserDto? opponent = await _accountService.GetUserByIdAsync(opponentId);
                Dictionary<string, UserDto> users = [];
                if (opponent != null) users[opponent.Id] = opponent;

                return ServiceResultWithData<BattleshipGameDto>.Success(
                    MapGame(game, currentUserId, users));
            });
        }

        public async Task<ServiceResult> ExpireOverdueGamesAsync(string currentUserId)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(currentUserId))
                    return ServiceResult.Failure(OperationStatus.Unauthorized,
                        "Debe iniciar sesión.");

                if (!await _userAccountStatusService.IsUserActiveAsync(currentUserId))
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "Su cuenta no se encuentra activa.");

                DateTime nowUtc = DateTime.UtcNow;
                DateTime limitUtc = nowUtc.AddHours(-48);

                await _gameRepository.GetAllQuery()
                    .Where(game =>
                        (game.CreatorUserId == currentUserId ||
                         game.OpponentUserId == currentUserId) &&
                        game.Status == BattleshipGameStatus.InProgress &&
                        game.TurnStartedAt != null &&
                        game.TurnStartedAt <= limitUtc &&
                        game.CurrentTurnUserId != null)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(game => game.Status, BattleshipGameStatus.Finished)
                        .SetProperty(game => game.WinnerUserId,
                            game => game.CurrentTurnUserId == game.CreatorUserId
                                ? game.OpponentUserId : game.CreatorUserId)
                        .SetProperty(game => game.EndReason,
                            (BattleshipGameEndReason?)BattleshipGameEndReason.Abandonment)
                        .SetProperty(game => game.CurrentTurnUserId, (string?)null)
                        .SetProperty(game => game.TurnStartedAt, (DateTime?)null)
                        .SetProperty(game => game.FinishedAt, (DateTime?)nowUtc));

                return ServiceResult.Success();
            });
        }

        public async Task<ServiceResult> SurrenderAsync(int gameId, string currentUserId)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(currentUserId))
                    return ServiceResult.Failure(OperationStatus.Unauthorized,
                        "Debe iniciar sesión.");

                if (!await _userAccountStatusService.IsUserActiveAsync(currentUserId))
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "Su cuenta no se encuentra activa.");

                if (gameId <= 0)
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "La partida seleccionada no es válida.");

                ServiceResult expiry = await ExpireOverdueGamesAsync(currentUserId);
                if (!expiry.IsSuccess) return expiry;

                DateTime nowUtc = DateTime.UtcNow;
                int changed = await _gameRepository.GetAllQuery()
                    .Where(game => game.Id == gameId &&
                        (game.CreatorUserId == currentUserId ||
                         game.OpponentUserId == currentUserId) &&
                        game.Status != BattleshipGameStatus.Finished)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(game => game.Status, BattleshipGameStatus.Finished)
                        .SetProperty(game => game.WinnerUserId,
                            game => game.CreatorUserId == currentUserId
                                ? game.OpponentUserId : game.CreatorUserId)
                        .SetProperty(game => game.EndReason,
                            (BattleshipGameEndReason?)BattleshipGameEndReason.Surrender)
                        .SetProperty(game => game.CurrentTurnUserId, (string?)null)
                        .SetProperty(game => game.TurnStartedAt, (DateTime?)null)
                        .SetProperty(game => game.FinishedAt, (DateTime?)nowUtc));

                if (changed == 1) return ServiceResult.Success();

                BattleshipGame? existing = await _gameRepository.GetAllQuery()
                    .AsNoTracking().FirstOrDefaultAsync(g => g.Id == gameId);

                if (existing == null)
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La partida no existe.");

                if (existing.CreatorUserId != currentUserId &&
                    existing.OpponentUserId != currentUserId)
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No posee permisos para finalizar esta partida.");

                return ServiceResult.Failure(OperationStatus.Conflict,
                    "La partida ya fue finalizada.");
            });
        }

        public async Task<ServiceResultWithData<BattleshipResultDto>> GetResultAsync(
            int gameId, string currentUserId)
        {
            return await ExecuteAsync<BattleshipResultDto>(async () =>
            {
                ServiceResultWithData<BattleshipResultDto>? invalid =
                    await ValidateCurrentUserAsync<BattleshipResultDto>(currentUserId);
                if (invalid != null) return invalid;

                ServiceResult expiry = await ExpireOverdueGamesAsync(currentUserId);
                if (!expiry.IsSuccess)
                    return ServiceResultWithData<BattleshipResultDto>.Failure(
                        expiry.Status, expiry.Errors.ToArray());

                BattleshipGame? game = await _gameRepository.GetAllQuery()
                    .AsNoTracking().FirstOrDefaultAsync(g => g.Id == gameId);

                if (game == null)
                    return ServiceResultWithData<BattleshipResultDto>.Failure(
                        OperationStatus.NotFound, "La partida no existe.");

                if (game.CreatorUserId != currentUserId &&
                    game.OpponentUserId != currentUserId)
                    return ServiceResultWithData<BattleshipResultDto>.Failure(
                        OperationStatus.Forbidden,
                        "No posee permisos para consultar esta partida.");

                if (game.Status != BattleshipGameStatus.Finished ||
                    game.FinishedAt == null || game.EndReason == null)
                    return ServiceResultWithData<BattleshipResultDto>.Failure(
                        OperationStatus.Conflict,
                        "Los resultados solo están disponibles después de finalizar.");

                string opponentId = game.CreatorUserId == currentUserId
                    ? game.OpponentUserId : game.CreatorUserId;
                UserDto? opponent = await _accountService.GetUserByIdAsync(opponentId);

                List<BattleshipAttackCellDto> mine = await _attackRepository.GetAllQuery()
                    .AsNoTracking().Where(a => a.GameId == gameId &&
                        a.AttackerUserId == currentUserId)
                    .OrderBy(a => a.AttackedAt)
                    .Select(a => new BattleshipAttackCellDto
                    {
                        Row = a.Row,
                        Column = a.Column,
                        IsHit = a.IsHit
                    }).ToListAsync();

                List<BattleshipAttackCellDto> theirs = await _attackRepository.GetAllQuery()
                    .AsNoTracking().Where(a => a.GameId == gameId &&
                        a.AttackerUserId == opponentId)
                    .OrderBy(a => a.AttackedAt)
                    .Select(a => new BattleshipAttackCellDto
                    {
                        Row = a.Row,
                        Column = a.Column,
                        IsHit = a.IsHit
                    }).ToListAsync();

                BattleshipResultDto dto = new()
                {
                    GameId = gameId,
                    OpponentUserName = opponent?.UserName ?? "Usuario no disponible",
                    CreatedAt = game.CreatedAt,
                    FinishedAt = game.FinishedAt.Value,
                    DurationHours = Math.Max(0,
                        (game.FinishedAt.Value - game.CreatedAt).TotalHours),
                    DidIWin = game.WinnerUserId == currentUserId,
                    EndReason = game.EndReason.Value,
                    MyAttacks = mine,
                    OpponentAttacks = theirs
                };
                return ServiceResultWithData<BattleshipResultDto>.Success(dto);
            });
        }

        private async Task<bool> HasUnfinishedGameAsync(string firstUserId, string secondUserId)
        {
            return await _gameRepository.GetAllQuery()
                .AsNoTracking()
                .AnyAsync(game => game.Status != BattleshipGameStatus.Finished &&
                    ((game.CreatorUserId == firstUserId &&
                      game.OpponentUserId == secondUserId) ||
                     (game.CreatorUserId == secondUserId &&
                      game.OpponentUserId == firstUserId)));
        }

        private async Task<ServiceResultWithData<T>?> ValidateCurrentUserAsync<T>(
            string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return ServiceResultWithData<T>.Failure(OperationStatus.Unauthorized,
                    "El usuario no está autenticado.");
            }

            if (!await _userAccountStatusService.IsUserActiveAsync(currentUserId))
            {
                return ServiceResultWithData<T>.Failure(OperationStatus.Forbidden,
                    "La cuenta del usuario no se encuentra activa.");
            }

            return null;
        }

        private static BattleshipGameDto MapGame(
            BattleshipGame game,
            string currentUserId,
            IReadOnlyDictionary<string, UserDto> users)
        {
            string opponentId = game.CreatorUserId == currentUserId
                ? game.OpponentUserId : game.CreatorUserId;

            users.TryGetValue(opponentId, out UserDto? opponent);

            return new BattleshipGameDto
            {
                Id = game.Id,
                OpponentUserId = opponentId,
                OpponentUserName = opponent?.UserName ?? "Usuario no disponible",
                OpponentProfileImage = opponent?.ProfileImage,
                Status = game.Status,
                CreatedAt = game.CreatedAt,
                BattleStartedAt = game.BattleStartedAt,
                FinishedAt = game.FinishedAt,
                CurrentTurnUserId = game.CurrentTurnUserId,
                WinnerUserId = game.WinnerUserId,
                EndReason = game.EndReason,
                IsCreator = game.CreatorUserId == currentUserId,
                IsMyTurn = game.Status == BattleshipGameStatus.InProgress &&
                           game.CurrentTurnUserId == currentUserId,
                DidIWin = game.Status == BattleshipGameStatus.Finished
                    ? game.WinnerUserId == currentUserId
                    : null
            };
        }
    }
}
