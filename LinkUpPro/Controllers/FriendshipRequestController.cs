using AutoMapper;
using LinkUpPro.Core.Application.Dtos.FriendshipRequest;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Application.ViewModels.FriendshipRequest;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{
    [Authorize]
    public class FriendshipRequestController : Controller
    {
        private readonly IFriendshipRequestService _friendshipRequestService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public FriendshipRequestController(
            IFriendshipRequestService friendshipRequestService,
            UserManager<AppUser> userManager,
            IMapper mapper)
        {
            _friendshipRequestService = friendshipRequestService;
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<List<FriendshipRequestListDto>> received =
                await _friendshipRequestService.GetReceivedPendingAsync(user.Id);
            if (!received.IsSuccess) return ReadFailure(received);

            ServiceResultWithData<List<FriendshipRequestListDto>> sent =
                await _friendshipRequestService.GetSentAsync(user.Id);
            if (!sent.IsSuccess) return ReadFailure(sent);

            ServiceResultWithData<int> pendingCount =
                await _friendshipRequestService.GetReceivedPendingCountAsync(user.Id);
            if (!pendingCount.IsSuccess) return ReadFailure(pendingCount);

            FriendshipRequestIndexViewModel vm = new()
            {
                ReceivedPendingCount = pendingCount.Data,
                ReceivedPending = _mapper.Map<List<FriendshipRequestListViewModel>>(
                    received.Data ?? []),
                Sent = _mapper.Map<List<FriendshipRequestListViewModel>>(sent.Data ?? [])
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> New(string? userNameFilter)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<List<AvailableFriendshipUserDto>> result =
                await _friendshipRequestService.GetAvailableUsersAsync(user.Id, userNameFilter);
            if (!result.IsSuccess) return ReadFailure(result);

            NewFriendshipRequestViewModel vm = new()
            {
                UserNameFilter = userNameFilter?.Trim(),
                Users = _mapper.Map<List<AvailableFriendshipUserViewModel>>(result.Data ?? [])
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(NewFriendshipRequestViewModel vm)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            if (ModelState.IsValid)
            {
                ServiceResult result = await _friendshipRequestService.SendAsync(
                    user.Id, vm.SelectedUserId!);

                if (result.IsSuccess)
                {
                    TempData["FriendshipRequestSuccess"] = "La solicitud fue enviada correctamente.";
                    return RedirectToAction(nameof(Index));
                }

                if (result.Status == OperationStatus.Unauthorized) return Challenge();
                if (result.Status == OperationStatus.Forbidden) return Forbid();

                foreach (string error in GetErrors(result, "No se pudo enviar la solicitud."))
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }

            // Volver a consultar: no confiar en la lista enviada por el navegador.
            ServiceResultWithData<List<AvailableFriendshipUserDto>> available =
                await _friendshipRequestService.GetAvailableUsersAsync(user.Id, vm.UserNameFilter);
            if (!available.IsSuccess) return ReadFailure(available);

            vm.Users = _mapper.Map<List<AvailableFriendshipUserViewModel>>(available.Data ?? []);
            return View("New", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Confirm(int requestId, string operation)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();
            if (requestId <= 0) return BadRequest();

            bool receivedOperation = operation is "Accept" or "Reject";
            bool sentOperation = operation is "Cancel" or "Hide";
            if (!receivedOperation && !sentOperation) return BadRequest();

            ServiceResultWithData<List<FriendshipRequestListDto>> list = receivedOperation
                ? await _friendshipRequestService.GetReceivedPendingAsync(user.Id)
                : await _friendshipRequestService.GetSentAsync(user.Id);

            if (!list.IsSuccess) return ReadFailure(list);

            FriendshipRequestListDto? request = list.Data?.FirstOrDefault(r => r.Id == requestId);
            if (request == null) return NotFound();

            if (operation == "Hide" &&
                request.Status is not (FriendshipRequestStatus.Accepted or FriendshipRequestStatus.Rejected))
            {
                return NotFound();
            }
            if (operation == "Cancel" && request.Status != FriendshipRequestStatus.Pending)
            {
                return NotFound();
            }

            string userName = request.OtherUser.UserName;
            FriendshipRequestConfirmationViewModel vm = new()
            {
                RequestId = requestId,
                Operation = operation,
                Message = operation switch
                {
                    "Accept" => $"¿Está seguro que desea aceptar la solicitud de amistad del usuario {userName}?",
                    "Reject" => $"¿Está seguro que desea rechazar la solicitud de amistad del usuario {userName}?",
                    "Cancel" => $"¿Está seguro que desea cancelar la solicitud de amistad enviada a {userName}?",
                    _ => "¿Está seguro que desea eliminar esta solicitud de su historial?"
                },
                ConfirmButtonText = operation switch
                {
                    "Accept" => "Aceptar",
                    "Reject" => "Rechazar",
                    "Cancel" => "Cancelar solicitud",
                    _ => "Eliminar"
                }
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Accept(int requestId) => RunOperationAsync(
            requestId,
            (id, userId) => _friendshipRequestService.AcceptAsync(id, userId),
            "La solicitud fue aceptada correctamente.");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Reject(int requestId) => RunOperationAsync(
            requestId,
            (id, userId) => _friendshipRequestService.RejectAsync(id, userId),
            "La solicitud fue rechazada correctamente.");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Cancel(int requestId) => RunOperationAsync(
            requestId,
            (id, userId) => _friendshipRequestService.CancelAsync(id, userId),
            "La solicitud fue cancelada correctamente.");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Hide(int requestId) => RunOperationAsync(
            requestId,
            (id, userId) => _friendshipRequestService.HideFromHistoryAsync(id, userId),
            "La solicitud fue eliminada de su historial.");

        [HttpGet]
        public async Task<IActionResult> MutualFriends(string otherUserId)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();

            ServiceResultWithData<List<FriendshipUserDto>> result =
                await _friendshipRequestService.GetMutualFriendsAsync(user.Id, otherUserId);
            if (!result.IsSuccess) return ReadFailure(result);

            List<FriendshipUserViewModel> vm =
                _mapper.Map<List<FriendshipUserViewModel>>(result.Data ?? []);
            return View(vm);
        }

        private async Task<IActionResult> RunOperationAsync(
            int requestId,
            Func<int, string, Task<ServiceResult>> operation,
            string successMessage)
        {
            AppUser? user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            if (!user.EmailConfirmed) return Forbid();
            if (requestId <= 0) return BadRequest();

            ServiceResult result = await operation(requestId, user.Id);
            if (result.IsSuccess)
            {
                TempData["FriendshipRequestSuccess"] = successMessage;
                return RedirectToAction(nameof(Index));
            }

            if (result.Status == OperationStatus.Unauthorized) return Challenge();
            if (result.Status == OperationStatus.Forbidden) return Forbid();
            if (result.Status == OperationStatus.NotFound) return NotFound();

            TempData["FriendshipRequestError"] = string.Join(" ", GetErrors(result,
                "No se pudo completar la operación. Vuelva a intentarlo."));
            return RedirectToAction(nameof(Index));
        }

        private IActionResult ReadFailure(ServiceResult result)
        {
            return result.Status switch
            {
                OperationStatus.Unauthorized => Challenge(),
                OperationStatus.Forbidden => Forbid(),
                OperationStatus.NotFound => NotFound(),
                OperationStatus.ValidationError => BadRequest(GetErrors(result, "Consulta inválida.")),
                _ => StatusCode(500, "No fue posible consultar las solicitudes de amistad.")
            };
        }

        private static IEnumerable<string> GetErrors(ServiceResult result, string fallback)
        {
            return result.Errors.Count > 0 ? result.Errors : [fallback];
        }
    }
}
