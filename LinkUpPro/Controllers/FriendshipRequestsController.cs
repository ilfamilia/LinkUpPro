using AutoMapper;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.ViewModels.Social;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers;

/// <summary>Translates validated request forms into authorized application service operations.</summary>
public sealed class FriendshipRequestsController(IFriendshipRequestService requests, IMapper mapper) : SocialController
{
    /// <summary>Displays received pending requests and visible sent history.</summary>
    [HttpGet]
    public async Task<IActionResult> Index() => View(new RequestListViewModel
    {
        Incoming = mapper.Map<List<RequestItemViewModel>>(await requests.GetIncomingAsync()),
        Sent = mapper.Map<List<RequestItemViewModel>>(await requests.GetSentAsync())
    });

    /// <summary>Displays eligible accounts without trusting their eligibility at submission time.</summary>
    [HttpGet]
    public async Task<IActionResult> New(string? search) => View(new NewRequestViewModel
    {
        Search = search, Candidates = mapper.Map<List<FriendItemViewModel>>(await requests.GetEligibleUsersAsync(search))
    });

    /// <summary>Sends a request using only the selected receiver and the trusted caller identity.</summary>
    [HttpPost]
    public async Task<IActionResult> New(NewRequestViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await requests.SendAsync(model.SelectedUserId!);
                TempData["Success"] = "La solicitud de amistad fue enviada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DomainRuleException exception) { ModelState.AddModelError("", exception.Message); }
        }
        model.Candidates = mapper.Map<List<FriendItemViewModel>>(await requests.GetEligibleUsersAsync(model.Search));
        return View(model);
    }

    /// <summary>Loads confirmation information only from the caller's currently visible requests.</summary>
    [HttpGet]
    public async Task<IActionResult> Confirm(int id, RequestOperation operation)
    {
        if (!Enum.IsDefined(operation)) return BadRequest();
        var list = operation is RequestOperation.Accept or RequestOperation.Reject
            ? await requests.GetIncomingAsync() : await requests.GetSentAsync();
        var request = list.SingleOrDefault(r => r.Id == id)
            ?? throw new DomainRuleException("Esta solicitud ya no se encuentra disponible.");
        return View(new RequestActionViewModel { Id = id, Operation = operation, OtherUserName = request.OtherUser.UserName });
    }

    /// <summary>Revalidates the model and delegates every state and ownership rule to the service.</summary>
    [HttpPost]
    public async Task<IActionResult> Respond(RequestActionViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        switch (model.Operation)
        {
            case RequestOperation.Accept:
                await requests.AcceptAsync(model.Id);
                TempData["Success"] = "La solicitud fue aceptada correctamente.";
                break;
            case RequestOperation.Reject:
                await requests.RejectAsync(model.Id);
                TempData["Success"] = "La solicitud fue rechazada correctamente.";
                break;
            case RequestOperation.Cancel:
                await requests.CancelAsync(model.Id);
                TempData["Success"] = "La solicitud fue cancelada correctamente.";
                break;
            case RequestOperation.Hide:
                await requests.HideAsync(model.Id);
                TempData["Success"] = "La solicitud fue eliminada de su historial.";
                break;
            default: return BadRequest();
        }
        return RedirectToAction(nameof(Index));
    }
}
