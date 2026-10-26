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
    public class BattleshipPlacementService : BaseService, IBattleshipPlacementService
    {
        private const int BoardSize = 12;
        private const int ShipsPerPlayer = 5;
        private readonly IGenericRepository<BattleshipGame> _gameRepository;
        private readonly IGenericRepository<BattleshipShip> _shipRepository;
        private readonly IUserAccountStatusService _userAccountStatusService;
        private readonly ITransactionManager _transactionManager;

        public BattleshipPlacementService(
            IGenericRepository<BattleshipGame> gameRepository,
            IGenericRepository<BattleshipShip> shipRepository,
            IUserAccountStatusService userAccountStatusService,
            ITransactionManager transactionManager)
        {
            _gameRepository = gameRepository;
            _shipRepository = shipRepository;
            _userAccountStatusService = userAccountStatusService;
            _transactionManager = transactionManager;
        }

        public async Task<ServiceResultWithData<BattleshipPlacementDto>> GetPlacementAsync(int gameId, string userId)
        {
            return await ExecuteAsync<BattleshipPlacementDto>(async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResultWithData<BattleshipPlacementDto>.Failure(
                        OperationStatus.Unauthorized, "Debe iniciar sesión.");

                if (!await _userAccountStatusService.IsUserActiveAsync(userId))
                    return ServiceResultWithData<BattleshipPlacementDto>.Failure(
                        OperationStatus.Forbidden, "Su cuenta no se encuentra activa.");

                BattleshipGame? game = await _gameRepository.GetAllQuery().AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == gameId);

                if (game == null)
                    return ServiceResultWithData<BattleshipPlacementDto>.Failure(
                        OperationStatus.NotFound, "La partida no existe.");

                if (!IsParticipant(game, userId))
                    return ServiceResultWithData<BattleshipPlacementDto>.Failure(
                        OperationStatus.Forbidden, "No posee permisos para consultar esta partida.");

                List<BattleshipShip> ships = await _shipRepository.GetAllQuery().AsNoTracking()
                    .Where(s => s.GameId == gameId)
                    .ToListAsync();
                List<BattleshipShip> myShips = ships.Where(s => s.UserId == userId).ToList();
                string opponentId = game.CreatorUserId == userId
                    ? game.OpponentUserId : game.CreatorUserId;

                HashSet<int> alreadyPlaced = myShips.Select(s => s.ShipNumber).ToHashSet();
                List<BattleshipShipOptionDto> remaining = Enumerable.Range(1, ShipsPerPlayer)
                    .Where(number => !alreadyPlaced.Contains(number))
                    .Select(number => new BattleshipShipOptionDto
                    {
                        ShipNumber = number,
                        Size = GetShipSize(number)
                    }).ToList();

                List<BattleshipCellDto> occupied = myShips
                    .SelectMany(GetOccupiedCells)
                    .Select(cell => new BattleshipCellDto
                    {
                        Row = cell.Row,
                        Column = cell.Column
                    }).ToList();

                return ServiceResultWithData<BattleshipPlacementDto>.Success(
                    new BattleshipPlacementDto
                    {
                        GameId = gameId,
                        Status = game.Status,
                        PlacedShipCount = myShips.Count,
                        MyShipsReady = myShips.Count == ShipsPerPlayer,
                        OpponentShipsReady = ships.Count(s => s.UserId == opponentId) == ShipsPerPlayer,
                        IsMyTurn = game.Status == BattleshipGameStatus.InProgress &&
                                   game.CurrentTurnUserId == userId,
                        RemainingShips = remaining,
                        OccupiedCells = occupied
                    });
            });
        }

        public async Task<ServiceResult> PlaceShipAsync(int gameId, string userId,
            int shipNumber, int startRow, int startColumn, BattleshipShipDirection direction)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return ServiceResult.Failure(OperationStatus.Unauthorized, "Debe iniciar sesión.");

                if (!await _userAccountStatusService.IsUserActiveAsync(userId))
                    return ServiceResult.Failure(OperationStatus.Forbidden, "Su cuenta no se encuentra activa.");

                if (gameId <= 0 || shipNumber is < 1 or > ShipsPerPlayer ||
                    !Enum.IsDefined(direction) ||
                    startRow is < 0 or >= BoardSize || startColumn is < 0 or >= BoardSize)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "Los datos del barco o de la casilla no son válidos.");
                }

                int shipSize = GetShipSize(shipNumber);
                List<(int Row, int Column)> requestedCells =
                    CalculateCells(startRow, startColumn, shipSize, direction);

                if (requestedCells.Any(p => p.Row is < 0 or >= BoardSize ||
                                            p.Column is < 0 or >= BoardSize))
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "El barco quedaría fuera del tablero. Cambie la casilla o la dirección.");
                }

                await _transactionManager.BeginTransactionAsync();
                bool committed = false;
                try
                {
                    int locked = await _gameRepository.GetAllQuery()
                        .Where(g => g.Id == gameId &&
                                    g.Status == BattleshipGameStatus.SettingUp &&
                                    (g.CreatorUserId == userId || g.OpponentUserId == userId))
                        .ExecuteUpdateAsync(setters => setters.SetProperty(g => g.Status, g => g.Status));

                    if (locked == 0)
                    {
                        BattleshipGame? existing = await _gameRepository.GetAllQuery()
                            .AsNoTracking().FirstOrDefaultAsync(g => g.Id == gameId);

                        if (existing == null)
                            return ServiceResult.Failure(OperationStatus.NotFound,
                                "La partida no existe.");
                        if (!IsParticipant(existing, userId))
                            return ServiceResult.Failure(OperationStatus.Forbidden,
                                "No posee permisos para colocar barcos en esta partida.");
                        return ServiceResult.Failure(OperationStatus.Conflict,
                            "La partida ya no se encuentra en fase de posicionamiento.");
                    }

                    BattleshipGame game = (await _gameRepository.GetAllQuery()
                        .AsNoTracking().FirstAsync(g => g.Id == gameId));

                    List<BattleshipShip> ships = await _shipRepository.GetAllQuery()
                        .AsNoTracking().Where(s => s.GameId == gameId).ToListAsync();
                    List<BattleshipShip> myShips = ships.Where(s => s.UserId == userId).ToList();

                    if (myShips.Any(s => s.ShipNumber == shipNumber))
                        return ServiceResult.Failure(OperationStatus.Conflict,
                            "Este barco ya fue posicionado.");

                    if (myShips.Count >= ShipsPerPlayer)
                        return ServiceResult.Failure(OperationStatus.Conflict,
                            "Ya posicionó sus cinco barcos.");

                    HashSet<(int Row, int Column)> existingCells = myShips
                        .SelectMany(GetOccupiedCells).ToHashSet();

                    if (requestedCells.Any(existingCells.Contains))
                    {
                        return ServiceResult.Failure(OperationStatus.ValidationError,
                            "Debe cambiar la celda o la dirección, porque el barco " +
                            "quedaría posicionado encima de otro barco.");
                    }

                    BattleshipShip ship = new()
                    {
                        Id = 0,
                        GameId = gameId,
                        UserId = userId,
                        ShipNumber = shipNumber,
                        Size = shipSize,
                        StartRow = startRow,
                        StartColumn = startColumn,
                        Direction = direction
                    };

                    await _shipRepository.AddAsync(ship);

                    string opponentId = game.CreatorUserId == userId
                        ? game.OpponentUserId : game.CreatorUserId;
                    int opponentShips = ships.Count(s => s.UserId == opponentId);

                    if (myShips.Count + 1 == ShipsPerPlayer && opponentShips == ShipsPerPlayer)
                    {
                        DateTime now = DateTime.UtcNow;
                        game.Status = BattleshipGameStatus.InProgress;
                        game.CurrentTurnUserId = game.CreatorUserId;
                        game.BattleStartedAt = now;
                        game.TurnStartedAt = now;

                        BattleshipGame? updated = await _gameRepository.UpdateAsync(game, gameId);
                        if (updated == null)
                            throw new InvalidOperationException("La partida desapareció durante la actualización.");
                    }

                    await _transactionManager.CommitAsync();
                    committed = true;
                    return ServiceResult.Success();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "Otro jugador actualizó la partida. Actualice la pantalla.");
                }
                catch (DbUpdateException)
                {
                    return ServiceResult.Failure(OperationStatus.Conflict,
                        "No fue posible colocar el barco. Actualice el tablero e inténtelo de nuevo.");
                }
                finally
                {
                    if (!committed)
                    {
                        try { await _transactionManager.RollbackAsync(); }
                        catch (InvalidOperationException) { /* CommitAsync ya liberó la transacción. */ }
                    }
                }
            });
        }

        private static bool IsParticipant(BattleshipGame game, string userId) =>
            game.CreatorUserId == userId || game.OpponentUserId == userId;

        private static int GetShipSize(int shipNumber) => shipNumber switch
        {
            1 => 2,
            2 or 3 => 3,
            4 => 4,
            5 => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(shipNumber))
        };

        private static List<(int Row, int Column)> GetOccupiedCells(BattleshipShip ship) =>
            CalculateCells(ship.StartRow, ship.StartColumn, ship.Size, ship.Direction);

        private static List<(int Row, int Column)> CalculateCells(int row, int column, 
            int size, BattleshipShipDirection direction)
        {
            (int DeltaRow, int DeltaColumn) step = direction switch
            {
                BattleshipShipDirection.Up => (-1, 0),
                BattleshipShipDirection.Down => (1, 0),
                BattleshipShipDirection.Left => (0, -1),
                BattleshipShipDirection.Right => (0, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };

            return Enumerable.Range(0, size)
                .Select(offset => (Row: row + offset * step.DeltaRow,
                                   Column: column + offset * step.DeltaColumn))
                .ToList();
        }
    }
}
