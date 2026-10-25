using AutoMapper;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.ViewModels.Battleship;
using LinkUpPro.ViewModels.Social;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers;

/// <summary>Handles the multi-screen Battleship workflow without calculating game rules in Web.</summary>
public sealed class BattleshipController(IBattleshipService games, IMapper mapper) : SocialController
{
    /// <summary>Displays active games, historical results, and win/loss totals.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var list = mapper.Map<List<GameSummaryViewModel>>(await games.GetGamesAsync());
        return View(new BattleshipIndexViewModel
        {
            Active = list.Where(g => g.Status != BattleshipStatus.Finished).ToList(),
            History = list.Where(g => g.Status == BattleshipStatus.Finished).OrderByDescending(g => g.CompletedAt).ToList()
        });
    }

    /// <summary>Lists eligible friends who do not share another unfinished game with the caller.</summary>
    [HttpGet]
    public async Task<IActionResult> New(string? search) => View(new NewGameViewModel
    {
        Search = search, Candidates = mapper.Map<List<PublicUserViewModel>>(await games.GetEligibleOpponentsAsync(search))
    });

    /// <summary>Creates a game after server-side eligibility checks and opens its setup workflow.</summary>
    [HttpPost]
    public async Task<IActionResult> New(NewGameViewModel model)
    {
        if (ModelState.IsValid)
        {
            try { return RedirectToAction(nameof(Game), new { id = await games.CreateAsync(model.OpponentId!) }); }
            catch (DomainRuleException exception) { ModelState.AddModelError("", exception.Message); }
        }
        model.Candidates = mapper.Map<List<PublicUserViewModel>>(await games.GetEligibleOpponentsAsync(model.Search));
        return View(model);
    }

    /// <summary>Resumes setup, waiting, combat, or completed results according to persisted state.</summary>
    [HttpGet]
    public async Task<IActionResult> Game(int id)
    {
        var model = await LoadAsync(id);
        if (model.Status == BattleshipStatus.Setup && model.UnplacedShipNumbers.Count > 0)
            return View("Ships", model);
        return View(model);
    }

    /// <summary>Displays selectable starting cells for one still-unplaced ship.</summary>
    [HttpGet]
    public async Task<IActionResult> SelectCell(int id, int? shipNumber)
    {
        var model = await LoadAsync(id);
        if (model.Status != BattleshipStatus.Setup || !shipNumber.HasValue || !model.UnplacedShipNumbers.Contains(shipNumber.Value))
            throw new DomainRuleException("El barco seleccionado ya no se encuentra disponible.");
        model.SelectedShipNumber = shipNumber.Value;
        return View(model);
    }

    /// <summary>Shows the final direction choice after validating the starting cell and ship.</summary>
    [HttpGet]
    public async Task<IActionResult> Direction(int id, int shipNumber, int row, int column)
    {
        var game = await LoadAsync(id);
        if (game.Status != BattleshipStatus.Setup || !game.UnplacedShipNumbers.Contains(shipNumber)
            || row < 0 || row > 11 || column < 0 || column > 11
            || game.OwnShipCells.Any(c => c.Row == row && c.Column == column))
            throw new DomainRuleException("Debe seleccionar una celda y un barco disponibles.");
        return View(new PlaceShipViewModel { Id = id, Version = game.Version, ShipNumber = shipNumber, Row = row, Column = column,
            OwnShipCells = game.OwnShipCells });
    }

    /// <summary>Places a ship using validated input; geometry and concurrency are checked by the service.</summary>
    [HttpPost]
    public async Task<IActionResult> Place(PlaceShipViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await games.PlaceShipAsync(model.Id, model.Version, model.ShipNumber, model.Row, model.Column, model.Direction!.Value);
                return RedirectToAction(nameof(Game), new { id = model.Id });
            }
            catch (DomainRuleException exception) { ModelState.AddModelError("", exception.Message); }
        }
        // Reload the preview from the server so the redisplayed form never trusts posted board data.
        model.OwnShipCells = (await LoadAsync(model.Id)).OwnShipCells;
        return View("Direction", model);
    }

    /// <summary>Submits one protected attack and reloads the authoritative game state.</summary>
    [HttpPost]
    public async Task<IActionResult> Attack(AttackViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await games.AttackAsync(model.Id, model.Version, model.Row, model.Column);
        return RedirectToAction(nameof(Game), new { id = model.Id });
    }

    /// <summary>Shows only the current participant's fleet placement.</summary>
    [HttpGet]
    public async Task<IActionResult> OwnBoard(int id)
    {
        var model = await LoadAsync(id);
        model.ShowOwnBoard = true;
        return View("Game", model);
    }

    /// <summary>Reveals the other player's attack history only after the game is complete.</summary>
    [HttpGet]
    public async Task<IActionResult> OpponentBoard(int id)
    {
        var model = await LoadAsync(id);
        if (model.Status != BattleshipStatus.Finished)
            throw new DomainRuleException("No posee permisos para visualizar el tablero del oponente durante la partida.");
        model.ShowOpponentAttacks = true;
        return View("Game", model);
    }

    /// <summary>Shows a surrender confirmation without changing the game.</summary>
    [HttpGet]
    public async Task<IActionResult> ConfirmSurrender(int id)
    {
        var model = await LoadAsync(id);
        if (model.Status == BattleshipStatus.Finished)
            throw new DomainRuleException("La partida ya finalizó.");
        return View(new GameActionViewModel { Id = id, Version = model.Version });
    }

    /// <summary>Completes surrender through an antiforgery-protected, version-checked operation.</summary>
    [HttpPost]
    public async Task<IActionResult> Surrender(GameActionViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await games.SurrenderAsync(model.Id, model.Version);
        TempData["Success"] = "La partida finalizó. Consulte el resultado en su historial.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Maps safe service output to a screen-specific model.</summary>
    private async Task<GameDetailsViewModel> LoadAsync(int id)
        => mapper.Map<GameDetailsViewModel>(await games.GetGameAsync(id));
}
