using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Core.Application.Services
{
    public class BattleshipAttackService : BaseService, IBattleshipAttackService
    {
        private const int BoardSize = 12;
        private const int ShipsPerPlayer = 5;
        private const int FleetCellCount = 17; // 2 + 3 + 3 + 4 + 5

        private readonly IGenericRepository<BattleshipGame> _gameRepository;
        private readonly IGenericRepository<BattleshipShip> _shipRepository;
        private readonly IGenericRepository<BattleshipAttack> _attackRepository;
        private readonly IUserAccountStatusService _userAccountStatusService;
        private readonly ITransactionManager _transactionManager;
        private readonly IBattleshipGameService _gameService;

        public BattleshipAttackService(
            IGenericRepository<BattleshipGame> gameRepository,
            IGenericRepository<BattleshipShip> shipRepository,
            IGenericRepository<BattleshipAttack> attackRepository,
            IUserAccountStatusService userAccountStatusService,
            ITransactionManager transactionManager,
            IBattleshipGameService gameService)
        {
            _gameRepository = gameRepository;
            _shipRepository = shipRepository;
            _attackRepository = attackRepository;
            _userAccountStatusService = userAccountStatusService;
            _transactionManager = transactionManager;
            _gameService = gameService;
        }

        public async Task<ServiceResultWithData<BattleshipAttackBoardDto>> GetAttackBoardAsync(
            int gameId, string userId)
        {
            return await ExecuteAsync<BattleshipAttackBoardDto>(async () =>
            {
                ServiceResultWithData<BattleshipAttackBoardDto>? error =
                    await ValidateViewerAsync<BattleshipAttackBoardDto>(userId);
                if (error != null) return error;

                ServiceResult expiry = await _gameService.ExpireOverdueGamesAsync(userId);
                if (!expiry.IsSuccess)
                    return ServiceResultWithData<BattleshipAttackBoardDto>.Failure(
                        expiry.Status, expiry.Errors.ToArray());

                BattleshipGame? game = await _gameRepository.GetAllQuery()
                    .AsNoTracking().FirstOrDefaultAsync(g => g.Id == gameId);

                if (game == null)
                    return ServiceResultWithData<BattleshipAttackBoardDto>.Failure(
                        OperationStatus.NotFound, "La partida no existe.");

                if (!IsParticipant(game, userId))
                    return ServiceResultWithData<BattleshipAttackBoardDto>.Failure(
                        OperationStatus.Forbidden, "No posee permisos para consultar esta partida.");

                if (game.Status == BattleshipGameStatus.SettingUp)
                    return ServiceResultWithData<BattleshipAttackBoardDto>.Failure(
                        OperationStatus.Conflict, "Primero deben posicionarse todos los barcos.");

                List<BattleshipAttackCellDto> myAttacks = await _attackRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(a => a.GameId == gameId && a.AttackerUserId == userId)
                    .OrderBy(a => a.AttackedAt)
                    .Select(a => new BattleshipAttackCellDto
                    {
                        Row = a.Row,
                        Column = a.Column,
                        IsHit = a.IsHit
                    }).ToListAsync();

                return ServiceResultWithData<BattleshipAttackBoardDto>.Success(
                    new BattleshipAttackBoardDto
                    {
                        GameId = gameId,
                        Status = game.Status,
                        CurrentTurnUserId = game.CurrentTurnUserId,
                        IsMyTurn = game.Status == BattleshipGameStatus.InProgress &&
                                   game.CurrentTurnUserId == userId,
                        DidIWin = game.Status == BattleshipGameStatus.Finished
                            ? game.WinnerUserId == userId : null,
                        Attacks = myAttacks
                    });
            });
        }

        public async Task<ServiceResultWithData<BattleshipOwnBoardDto>> GetOwnBoardAsync(
            int gameId, string userId)
        {
            return await ExecuteAsync<BattleshipOwnBoardDto>(async () =>
            {
                ServiceResultWithData<BattleshipOwnBoardDto>? error =
                    await ValidateViewerAsync<BattleshipOwnBoardDto>(userId);
                if (error != null) return error;

                ServiceResult expiry = await _gameService.ExpireOverdueGamesAsync(userId);
                if (!expiry.IsSuccess)
                    return ServiceResultWithData<BattleshipOwnBoardDto>.Failure(
                        expiry.Status, expiry.Errors.ToArray());

                BattleshipGame? game = await _gameRepository.GetAllQuery()
                    .AsNoTracking().FirstOrDefaultAsync(g => g.Id == gameId);

                if (game == null)
                    return ServiceResultWithData<BattleshipOwnBoardDto>.Failure(
                        OperationStatus.NotFound, "La partida no existe.");

                if (!IsParticipant(game, userId))
                    return ServiceResultWithData<BattleshipOwnBoardDto>.Failure(
                        OperationStatus.Forbidden, "No posee permisos para consultar esta partida.");

                string opponentId = GetOpponentUserId(game, userId);
                List<BattleshipShip> myShips = await _shipRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(s => s.GameId == gameId && s.UserId == userId)
                    .ToListAsync();

                List<BattleshipAttackCellDto> received = await _attackRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(a => a.GameId == gameId && a.AttackerUserId == opponentId)
                    .Select(a => new BattleshipAttackCellDto
                    {
                        Row = a.Row,
                        Column = a.Column,
                        IsHit = a.IsHit
                    }).ToListAsync();

                return ServiceResultWithData<BattleshipOwnBoardDto>.Success(
                    new BattleshipOwnBoardDto
                    {
                        GameId = gameId,
                        Status = game.Status,
                        OccupiedCells = myShips.SelectMany(GetShipCells)
                            .Select(c => new BattleshipCellDto
                            {
                                Row = c.Row,
                                Column = c.Column
                            }).ToList(),
                        ReceivedAttacks = received
                    });
            });
        }

        public async Task<ServiceResult> AttackAsync(
            int gameId, string attackerUserId, int row, int column)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(attackerUserId))
                    return ServiceResult.Failure(OperationStatus.Unauthorized,
                        "Debe iniciar sesión.");

                if (!await _userAccountStatusService.IsUserActiveAsync(attackerUserId))
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "Su cuenta no se encuentra activa.");

                if (gameId <= 0 || row is < 0 or >= BoardSize ||
                    column is < 0 or >= BoardSize)
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "Seleccione una casilla válida del tablero.");

                ServiceResult expiry = await _gameService.ExpireOverdueGamesAsync(attackerUserId);
                if (!expiry.IsSuccess) return expiry;

                await _transactionManager.BeginTransactionAsync();
                bool committed = false;
                try
                {
                    DateTime attackDeadlineUtc = DateTime.UtcNow.AddHours(-48);
                    int locked = await _gameRepository.GetAllQuery()
                        .Where(g => g.Id == gameId &&
                                    g.Status == BattleshipGameStatus.InProgress &&
                                    g.CurrentTurnUserId == attackerUserId &&
                                    g.TurnStartedAt != null &&
                                    g.TurnStartedAt > attackDeadlineUtc)
                        .ExecuteUpdateAsync(setters =>
                            setters.SetProperty(g => g.Status, g => g.Status));

                    if (locked == 0)
                    {
                        BattleshipGame? existing = await _gameRepository.GetAllQuery()
                            .AsNoTracking().FirstOrDefaultAsync(g => g.Id == gameId);

                        if (existing == null)
                            return ServiceResult.Failure(OperationStatus.NotFound,
                                "La partida no existe.");

                        if (!IsParticipant(existing, attackerUserId))
                            return ServiceResult.Failure(OperationStatus.Forbidden,
                                "No posee permisos para atacar en esta partida.");

                        if (existing.Status != BattleshipGameStatus.InProgress)
                            return ServiceResult.Failure(OperationStatus.Conflict,
                                "La partida no se encuentra en fase de ataques.");

                        return ServiceResult.Failure(OperationStatus.Conflict,
                            "No es su turno de atacar. Refresque la pantalla.");
                    }

                    BattleshipGame game = await _gameRepository.GetAllQuery()
                        .AsNoTracking().FirstAsync(g => g.Id == gameId);
                    string opponentId = GetOpponentUserId(game, attackerUserId);

                    List<BattleshipAttack> previousAttacks = await _attackRepository
                        .GetAllQuery().AsNoTracking()
                        .Where(a => a.GameId == gameId && a.AttackerUserId == attackerUserId)
                        .ToListAsync();

                    if (previousAttacks.Any(a => a.Row == row && a.Column == column))
                        return ServiceResult.Failure(OperationStatus.Conflict,
                            "Ya atacó esa casilla anteriormente.");

                    List<BattleshipShip> opponentShips = await _shipRepository
                        .GetAllQuery().AsNoTracking()
                        .Where(s => s.GameId == gameId && s.UserId == opponentId)
                        .ToListAsync();

                    HashSet<(int Row, int Column)> opponentCells = opponentShips
                        .SelectMany(GetShipCells).ToHashSet();
                    if (opponentShips.Count != ShipsPerPlayer ||
                        opponentCells.Count != FleetCellCount)
                        return ServiceResult.Failure(OperationStatus.Conflict,
                            "Los barcos del rival no están listos para recibir ataques.");

                    bool hit = opponentCells.Contains((row, column));
                    HashSet<(int Row, int Column)> previousHits = previousAttacks
                        .Where(a => a.IsHit)
                        .Select(a => (a.Row, a.Column)).ToHashSet();

                    if (hit) previousHits.Add((row, column));
                    bool allShipsSunk = opponentCells.All(previousHits.Contains);
                    DateTime now = DateTime.UtcNow;

                    BattleshipAttack attack = new()
                    {
                        Id = 0,
                        GameId = gameId,
                        AttackerUserId = attackerUserId,
                        Row = row,
                        Column = column,
                        IsHit = hit,
                        AttackedAt = now
                    };
                    
                    await _attackRepository.AddAsync(attack);

                    if (allShipsSunk)
                    {
                        game.Status = BattleshipGameStatus.Finished;
                        game.CurrentTurnUserId = null;
                        game.TurnStartedAt = null;
                        game.WinnerUserId = attackerUserId;
                        game.EndReason = BattleshipGameEndReason.AllShipsSunk;
                        game.FinishedAt = now;
                    }
                    else
                    {
                        game.CurrentTurnUserId = opponentId;
                        game.TurnStartedAt = now;
                    }

                    BattleshipGame? saved = await _gameRepository.UpdateAsync(game, gameId);
                    if (saved == null)
                        throw new InvalidOperationException(
                            "La partida desapareció durante el registro del ataque.");

                    await _transactionManager.CommitAsync();
                    committed = true;
                    return ServiceResult.Success();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "Otro jugador modificó la partida. Refresque la pantalla.");
                }
                catch (DbUpdateException)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "No fue posible registrar el ataque. Refresque la pantalla.");
                }
                finally
                {
                    if (!committed)
                    {
                        try { await _transactionManager.RollbackAsync(); }
                        catch (InvalidOperationException) { /* La transacción ya no está activa. */ }
                    }
                }
            });
        }

        private async Task<ServiceResultWithData<T>?> ValidateViewerAsync<T>(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return ServiceResultWithData<T>.Failure(OperationStatus.Unauthorized,
                    "Debe iniciar sesión.");

            if (!await _userAccountStatusService.IsUserActiveAsync(userId))
                return ServiceResultWithData<T>.Failure(OperationStatus.Forbidden,
                    "Su cuenta no se encuentra activa.");

            return null;
        }

        private static bool IsParticipant(BattleshipGame game, string userId) =>
            game.CreatorUserId == userId || game.OpponentUserId == userId;

        private static string GetOpponentUserId(BattleshipGame game, string userId) =>
            game.CreatorUserId == userId ? game.OpponentUserId : game.CreatorUserId;

        private static IEnumerable<(int Row, int Column)> GetShipCells(BattleshipShip ship)
        {
            (int deltaRow, int deltaColumn) = ship.Direction switch
            {
                BattleshipShipDirection.Up => (-1, 0),
                BattleshipShipDirection.Down => (1, 0),
                BattleshipShipDirection.Left => (0, -1),
                BattleshipShipDirection.Right => (0, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(ship.Direction))
            };

            for (int offset = 0; offset < ship.Size; offset++)
                yield return (ship.StartRow + deltaRow * offset,
                             ship.StartColumn + deltaColumn * offset);
        }
    }
}
