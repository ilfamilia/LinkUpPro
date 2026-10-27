using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.Core.Application.Dtos.Friendship;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Application.ViewModels.Battleship;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{
    [Authorize]
    public class BattleshipController : Controller
    {
        private readonly IBattleshipGameService _gameService;
        private readonly IBattleshipPlacementService _placementService;
        private readonly IBattleshipAttackService _attackService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public BattleshipController(
            IBattleshipGameService gameService,
            IBattleshipPlacementService placementService,
            IBattleshipAttackService attackService,
            UserManager<AppUser> userManager,
            IMapper mapper)
        {
            _gameService = gameService;
            _placementService = placementService;
            _attackService = attackService;
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<BattleshipGameListDto> result =
                await _gameService.GetMyGamesAsync(user.Id);

            if (!result.IsSuccess || result.Data == null)
                return ReadFailure(result);

            BattleshipIndexViewModel vm =
                _mapper.Map<BattleshipIndexViewModel>(result.Data);

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> New(string? searchText)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<List<FriendDto>> result =
                await _gameService.GetAvailableOpponentsAsync(user.Id, searchText);

            if (!result.IsSuccess) return ReadFailure(result);

            NewBattleshipGameViewModel vm = new()
            {
                SearchText = searchText?.Trim(),
                Opponents = MapOpponents(result.Data ?? [], searchText)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewBattleshipGameViewModel vm)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            if (ModelState.IsValid)
            {
                ServiceResultWithData<int> result = await _gameService.CreateAsync(
                    user.Id, vm.SelectedOpponentUserId!);

                if (result.IsSuccess)
                {
                    TempData["BattleshipSuccess"] = "La partida fue creada correctamente.";
                    return RedirectToAction(nameof(Setup), new { gameId = result.Data });
                }

                if (result.Status == OperationStatus.Unauthorized) return Challenge();
                if (result.Status == OperationStatus.Forbidden) return Forbid();

                foreach (string error in result.Errors)
                    ModelState.AddModelError(string.Empty, error);
            }

            ServiceResultWithData<List<FriendDto>> available =
                await _gameService.GetAvailableOpponentsAsync(user.Id, vm.SearchText);

            if (!available.IsSuccess) return ReadFailure(available);

            vm.Opponents = MapOpponents(available.Data ?? [], vm.SearchText);
            return View("New", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int gameId)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<BattleshipGameDto> result =
                await _gameService.GetGameAsync(gameId, user.Id);

            if (!result.IsSuccess || result.Data == null)
                return ReadFailure(result);

            if (result.Data.Status == BattleshipGameStatus.SettingUp)
                return RedirectToAction(nameof(Setup), new { gameId });

            if (result.Data.Status == BattleshipGameStatus.InProgress)
                return RedirectToAction(nameof(AttackBoard), new { gameId });

            return RedirectToAction(nameof(Result), new { gameId });
        }

        [HttpGet]
        public async Task<IActionResult> Setup(int gameId)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<BattleshipPlacementDto> result =
                await _placementService.GetPlacementAsync(gameId, user.Id);
            if (!result.IsSuccess || result.Data == null) return ReadFailure(result);

            if (result.Data.Status != BattleshipGameStatus.SettingUp)
                return RedirectToAction(nameof(Details), new { gameId });

            return View(_mapper.Map<BattleshipPlacementViewModel>(result.Data));
        }

        [HttpGet]
        public async Task<IActionResult> SelectCell(int gameId, int shipNumber)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<BattleshipPlacementDto> result =
                await _placementService.GetPlacementAsync(gameId, user.Id);
            if (!result.IsSuccess || result.Data == null) return ReadFailure(result);

            BattleshipShipOptionDto? option = result.Data.RemainingShips
                .FirstOrDefault(s => s.ShipNumber == shipNumber);

            if (result.Data.Status != BattleshipGameStatus.SettingUp || option == null)
            {
                TempData["BattleshipPlacementError"] = "El barco seleccionado ya no está disponible.";
                return RedirectToAction(nameof(Setup), new { gameId });
            }

            return View(new BattleshipSelectCellViewModel
            {
                Placement = _mapper.Map<BattleshipPlacementViewModel>(result.Data),
                ShipNumber = shipNumber,
                ShipSize = option.Size
            });
        }

        [HttpGet]
        public async Task<IActionResult> SelectDirection(int gameId,
            int shipNumber, int row, int column)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<BattleshipPlacementDto> result =
                await _placementService.GetPlacementAsync(gameId, user.Id);
            if (!result.IsSuccess || result.Data == null) return ReadFailure(result);

            bool shipAvailable = result.Data.RemainingShips
                .Any(s => s.ShipNumber == shipNumber);
            bool occupied = result.Data.OccupiedCells
                .Any(c => c.Row == row && c.Column == column);

            if (result.Data.Status != BattleshipGameStatus.SettingUp ||
                !shipAvailable || row is < 0 or > 11 || column is < 0 or > 11 || occupied)
            {
                TempData["BattleshipPlacementError"] =
                    "El barco o la casilla ya no está disponible.";
                return RedirectToAction(nameof(Setup), new { gameId });
            }

            return View(new BattleshipPlaceShipViewModel
            {
                GameId = gameId,
                ShipNumber = shipNumber,
                Row = row,
                Column = column
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceShip(BattleshipPlaceShipViewModel vm)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["BattleshipPlacementError"] =
                    string.Join(" ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(SelectDirection), new
                {
                    gameId = vm.GameId,
                    shipNumber = vm.ShipNumber,
                    row = vm.Row,
                    column = vm.Column
                });
            }

            ServiceResult result = await _placementService.PlaceShipAsync(
                vm.GameId, user.Id, vm.ShipNumber, vm.Row,
                vm.Column, vm.Direction!.Value);

            if (result.IsSuccess)
            {
                TempData["BattleshipPlacementSuccess"] = "Barco colocado correctamente.";
                return RedirectToAction(nameof(Setup), new { gameId = vm.GameId });
            }

            if (result.Status == OperationStatus.Unauthorized) return Challenge();
            if (result.Status == OperationStatus.Forbidden) return Forbid();
            if (result.Status == OperationStatus.NotFound) return NotFound();

            TempData["BattleshipPlacementError"] = result.Errors.Count > 0
                ? string.Join(" ", result.Errors)
                : "No fue posible colocar el barco.";

            return result.Status == OperationStatus.ValidationError
                ? RedirectToAction(nameof(SelectDirection), new
                {
                    gameId = vm.GameId,
                    shipNumber = vm.ShipNumber,
                    row = vm.Row,
                    column = vm.Column
                })
                : RedirectToAction(nameof(Setup), new { gameId = vm.GameId });
        }


        [HttpGet]
        public async Task<IActionResult> AttackBoard(int gameId)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<BattleshipAttackBoardDto> result =
                await _attackService.GetAttackBoardAsync(gameId, user.Id);

            if (result.Status == OperationStatus.Conflict)
                return RedirectToAction(nameof(Setup), new { gameId });
            if (!result.IsSuccess || result.Data == null)
                return ReadFailure(result);

            return View(_mapper.Map<BattleshipAttackBoardViewModel>(result.Data));
        }

        [HttpGet]
        public async Task<IActionResult> MyBoard(int gameId)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<BattleshipOwnBoardDto> result =
                await _attackService.GetOwnBoardAsync(gameId, user.Id);

            if (!result.IsSuccess || result.Data == null)
                return ReadFailure(result);

            return View(_mapper.Map<BattleshipOwnBoardViewModel>(result.Data));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Attack(BattleshipAttackViewModel vm)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["BattleshipAttackError"] =
                    "Seleccione una casilla válida del tablero.";
                return RedirectToAction(nameof(AttackBoard), new { gameId = vm.GameId });
            }

            ServiceResult result = await _attackService.AttackAsync(
                vm.GameId, user.Id, vm.Row, vm.Column);

            if (result.Status == OperationStatus.Unauthorized) return Challenge();
            if (result.Status == OperationStatus.Forbidden) return Forbid();
            if (result.Status == OperationStatus.NotFound) return NotFound();

            TempData[result.IsSuccess ? "BattleshipAttackSuccess" : "BattleshipAttackError"] =
                result.IsSuccess ? "Ataque registrado."
                    : result.Errors.Count > 0 ? string.Join(" ", result.Errors)
                    : "No se pudo registrar el ataque.";

            return RedirectToAction(nameof(AttackBoard), new { gameId = vm.GameId });
        }

        [HttpGet]
        public async Task<IActionResult> Surrender(int gameId)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<BattleshipGameDto> result =
                await _gameService.GetGameAsync(gameId, user.Id);
            if (!result.IsSuccess || result.Data == null) return ReadFailure(result);

            if (result.Data.Status == BattleshipGameStatus.Finished)
                return RedirectToAction(nameof(Result), new { gameId });

            return View(new BattleshipSurrenderViewModel
            {
                GameId = gameId,
                OpponentUserName = result.Data.OpponentUserName
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmSurrender(int gameId)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResult result = await _gameService.SurrenderAsync(gameId, user.Id);
            if (result.IsSuccess)
            {
                TempData["BattleshipSuccess"] =
                    "Te has rendido. La partida fue finalizada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            if (result.Status == OperationStatus.Unauthorized) return Challenge();
            if (result.Status == OperationStatus.Forbidden) return Forbid();
            if (result.Status == OperationStatus.NotFound) return NotFound();

            TempData["BattleshipError"] = result.Errors.Count > 0
                ? string.Join(" ", result.Errors) : "No se pudo finalizar la partida.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public Task<IActionResult> Result(int gameId) =>
            ShowResultAsync(gameId, showOpponentBoard: false);

        [HttpGet]
        public Task<IActionResult> OpponentResult(int gameId) =>
            ShowResultAsync(gameId, showOpponentBoard: true);

        private async Task<IActionResult> ShowResultAsync(int gameId, bool showOpponentBoard)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<BattleshipResultDto> result =
                await _gameService.GetResultAsync(gameId, user.Id);

            if (result.Status == OperationStatus.Conflict)
                return RedirectToAction(nameof(Details), new { gameId });

            if (!result.IsSuccess || result.Data == null)
                return ReadFailure(result);

            BattleshipResultViewModel vm =
                _mapper.Map<BattleshipResultViewModel>(result.Data);
            vm.ShowOpponentBoard = showOpponentBoard;
            return View("Result", vm);
        }

        private List<BattleshipOpponentViewModel> MapOpponents(
            IEnumerable<FriendDto> friends, string? searchText)
        {
            IEnumerable<FriendDto> filtered = friends;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string term = searchText.Trim();
                filtered = filtered.Where(friend =>
                    friend.UserName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            return _mapper.Map<List<BattleshipOpponentViewModel>>(filtered.ToList());
        }

        private IActionResult ReadFailure(ServiceResult result)
        {
            return result.Status switch
            {
                OperationStatus.Unauthorized => Challenge(),
                OperationStatus.Forbidden => Forbid(),
                OperationStatus.NotFound => NotFound(),
                OperationStatus.ValidationError => BadRequest(result.Errors),
                OperationStatus.Conflict => Conflict(result.Errors),
                _ => Problem("No fue posible completar la consulta de Battleship.")
            };
        }
    }
}
