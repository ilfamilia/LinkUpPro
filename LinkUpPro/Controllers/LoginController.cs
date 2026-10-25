using AutoMapper;
using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.ViewModels.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{
    // Public account screens. Each anonymous action is marked explicitly; logout requires a session.
    public class LoginController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IAccountServiceForWebApp _accountServiceForWebApp;
        private readonly IImageStorageService _imageStorage;

        public LoginController(IMapper mapper, IAccountServiceForWebApp accountServiceForWebApp,
            IImageStorageService imageStorage)
        {
            _mapper = mapper;
            _accountServiceForWebApp = accountServiceForWebApp;
            _imageStorage = imageStorage;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Index(string? reason)
        {
            if (IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Info = reason switch
            {
                "timeout" => "Su sesión finalizó por inactividad. Inicie sesión nuevamente para continuar.",
                "required" => "Debe iniciar sesión para acceder a esta sección.",
                _ => null
            };

            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel vm)
        {
            if (IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                vm.Password = "";
                return View(vm);
            }

            LoginResponseDto response = await _accountServiceForWebApp.AuthenticateAsync(_mapper.Map<LoginDto>(vm));

            if (!response.HasError)
            {
                return RedirectToAction("Index", "Home");
            }

            foreach (string error in response.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            vm.Password = "";
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _accountServiceForWebApp.SignOutAsync();
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterUserViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterUserViewModel vm)
        {
            if (IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                if (await _accountServiceForWebApp.IsUserNameTakenAsync(vm.UserName))
                {
                    ModelState.AddModelError(nameof(vm.UserName), "Este nombre de usuario ya se encuentra registrado.");
                }

                if (await _accountServiceForWebApp.IsEmailTakenAsync(vm.Email))
                {
                    ModelState.AddModelError(nameof(vm.Email), "Este correo electrónico ya se encuentra registrado.");
                }
            }

            if (!ModelState.IsValid)
            {
                return RegisterView(vm);
            }

            // The id is generated first so the photo is stored in the new user's own folder.
            string userId = Guid.NewGuid().ToString();
            string imagePath;

            try
            {
                imagePath = await _imageStorage.SaveAsync(vm.ProfileImageFile!, userId, "Users");
            }
            catch (InvalidDataException ex)
            {
                ModelState.AddModelError(nameof(vm.ProfileImageFile), ex.Message);
                return RegisterView(vm);
            }
            catch (IOException)
            {
                ModelState.AddModelError(nameof(vm.ProfileImageFile), "No fue posible guardar la foto de perfil. Inténtelo nuevamente.");
                return RegisterView(vm);
            }

            RegisterUserDto dto = _mapper.Map<RegisterUserDto>(vm);
            dto.Id = userId;
            dto.ProfileImage = imagePath;

            RegisterResponseDto response = await _accountServiceForWebApp.RegisterUserAsync(dto, Origin);

            if (response.HasError)
            {
                // Never keep a photo that is not attached to a created profile.
                _imageStorage.Delete(imagePath);

                foreach (string error in response.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return RegisterView(vm);
            }

            return RedirectToAction(nameof(Registered));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Registered()
            => Notice("Cuenta creada",
                "Su cuenta fue creada correctamente. Hemos enviado un enlace de activación a su correo electrónico.", true);

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            UserResponseDto response = await _accountServiceForWebApp.ConfirmAccountAsync(userId, token);

            if (response.HasError)
            {
                return Notice("Activación de cuenta", response.Errors[0], false);
            }

            TempData["LoginSuccess"] = response.Message;
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResendEmailActivation()
        {
            if (IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new EmailConfirmationViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailActivation(EmailConfirmationViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            UserResponseDto response = await _accountServiceForWebApp.ResendEmailActivationAsync(
                _mapper.Map<EmailConfirmationDto>(vm), Origin);

            if (response.HasError)
            {
                ModelState.AddModelError(string.Empty, response.Errors[0]);
                return View(vm);
            }

            TempData["LoginSuccess"] = response.Message;
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new ForgotPasswordRequestViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            UserResponseDto response = await _accountServiceForWebApp.ForgotPasswordAsync(
                new ForgotPasswordRequestDto { UserName = vm.UserName, Origin = Origin });

            TempData["LoginSuccess"] = response.Message;
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string userId, string token)
        {
            UserResponseDto check = await _accountServiceForWebApp.ValidateResetTokenAsync(userId, token);

            if (check.HasError)
            {
                return Notice("Restablecer contraseña", check.Errors[0], false);
            }

            return View(new ResetPasswordRequestViewModel { Id = userId, Token = token });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            UserResponseDto response = await _accountServiceForWebApp.ResetPasswordAsync(
                _mapper.Map<ResetPasswordRequestDto>(vm));

            if (response.HasError)
            {
                foreach (string error in response.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View(vm);
            }

            // A reset invalidates the current browser session too.
            if (IsAuthenticated)
            {
                await _accountServiceForWebApp.SignOutAsync();
            }

            TempData["LoginSuccess"] = response.Message;
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View("~/Views/Shared/SocialError.cshtml", new LinkUpPro.ViewModels.Social.SocialErrorViewModel
            {
                Message = "No posee permisos para realizar esta acción."
            });
        }

        private bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

        // Links in emails point back to this same site.
        private string Origin => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        private IActionResult RegisterView(RegisterUserViewModel vm)
        {
            vm.Password = "";
            vm.ConfirmPassword = "";
            return View(vm);
        }

        private IActionResult Notice(string title, string message, bool success)
        {
            ViewData["NoticeTitle"] = title;
            ViewData["NoticeSuccess"] = success;
            return View("Notice", message);
        }
    }
}
