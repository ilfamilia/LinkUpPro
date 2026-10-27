using AutoMapper;
using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.ViewModels.User;
using LinkUpPro.Helpers;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{
    public class LoginController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IAccountServiceForWebApp _accountServiceForWebApp;
        private readonly UserManager<AppUser> _userManager;
        public LoginController(IMapper mapper, IAccountServiceForWebApp accountServiceForWebApp, UserManager<AppUser> userManager)
        {
            _mapper = mapper;
            _accountServiceForWebApp = accountServiceForWebApp;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index([FromQuery] bool sessionExpired = false)
        {
            AppUser? userSession = await _userManager.GetUserAsync(User);

            if (userSession != null)
            {
                var user = await _accountServiceForWebApp.GetUserByUserNameAsync(userSession.UserName ?? "");

                if (user != null)
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewData["SessionExpired"] = sessionExpired;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel vm)
        {
            AppUser? userSession = await _userManager.GetUserAsync(User);

            if(userSession != null)
            {
                var user = await _accountServiceForWebApp.GetUserByUserNameAsync(userSession.UserName ?? "");
                if(user != null){
                    return RedirectToAction("Index", "Home");
                }
                return RedirectToAction("Index", "Login");
            }

            if(!ModelState.IsValid)
            {
                vm.Password = "";
                return View(vm);
            }

            LoginDto loginDto = _mapper.Map<LoginDto>(vm);
            LoginResponseDto? LoginResponse = await _accountServiceForWebApp.AuthenticateAsync(loginDto);

            if(LoginResponse != null && !LoginResponse.HasError)
            {
                return RedirectToRoute(new { controller = "Home", action = "Index" });
            }
            else
            {
                foreach (string error in LoginResponse?.Errors ?? [])
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }
            vm.Password = "";
            return View(vm);
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _accountServiceForWebApp.SignOutAsync();

            return RedirectToAction("Index", "Login");
        }


        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterUserViewModel vm)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            vm.Name = vm.Name.Trim();
            vm.LastName = vm.LastName.Trim();
            vm.Email = vm.Email.Trim();
            vm.UserName = vm.UserName.Trim();
            vm.Phone = vm.Phone?.Trim();

            RegisterUserDto registerUserDto = _mapper.Map<RegisterUserDto>(vm);

            registerUserDto.Id = Guid.NewGuid().ToString();

            string imagePath;

            try
            {
                imagePath = FileManager.Upload(vm.ProfileImageFile, registerUserDto.Id, "Users");
            }
            catch (InvalidDataException ex)
            {
                ModelState.AddModelError(nameof(vm.ProfileImageFile), ex.Message);

                return View(vm);
            }
            catch (IOException)
            {
                ModelState.AddModelError(nameof(vm.ProfileImageFile), "No se pudo guardar la fotografía de perfil.");

                return View(vm);
            }
            catch (UnauthorizedAccessException)
            {
                ModelState.AddModelError(nameof(vm.ProfileImageFile), "No se pudo guardar la fotografía de perfil.");

                return View(vm);
            }

            registerUserDto.ProfileImage = imagePath;

            string origin = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

            RegisterResponseDto? registerUserResponse = await _accountServiceForWebApp.RegisterUserAsync(registerUserDto, origin);

            if (registerUserResponse == null || registerUserResponse.HasError)
            {
                FileManager.DeleteFile(imagePath);

                if (registerUserResponse == null)
                {
                    ModelState.AddModelError(string.Empty, "Ocurrió un error al registrar el usuario.");
                }
                else
                {
                    foreach (string error in registerUserResponse.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }
                }

                return View(vm);
            }

            TempData["RegistrationEmailSent"] = registerUserResponse.ActivationEmailSent.ToString();

            return RedirectToAction(nameof(RegistrationResult));
        }

        [AllowAnonymous]
        public IActionResult ResendEmailActivation()
        {
            return View(new EmailConfirmationViewModel
            {
                UserName = string.Empty
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailActivation(EmailConfirmationViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            EmailConfirmationDto dto = new()
            {
                UserName = vm.UserName.Trim()
            };

            string origin = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

            await _accountServiceForWebApp.ResendEmailActivationAsync(dto, origin);

            TempData["ActivationResendMessage"] = "Si la cuenta existe y todavía no ha sido activada, " +
                "recibirá un nuevo enlace de activación.";

            return RedirectToAction(nameof(ResendEmailActivation));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            string response = await _accountServiceForWebApp.ConfirmAccountAsync(userId, token);

            return View("ConfirmEmail", response);
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordRequestViewModel
            {
                UserName = string.Empty
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            ForgotPasswordRequestDto dto = new()
            {
                UserName = vm.UserName.Trim(),
                Origin = $"{Request.Scheme}://{Request.Host}{Request.PathBase}"
            };

            await _accountServiceForWebApp.ForgotPasswordAsync(dto);

            TempData["ForgotPasswordMessage"] = "Si el nombre de usuario corresponde a una cuenta " +
                "registrada, recibirá un enlace para restablecer " +
                "su contraseña.";

            return RedirectToAction(nameof(ForgotPassword));
        }

        [AllowAnonymous]
        public IActionResult ResetPassword(string? userId, string? token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                TempData["PasswordResetError"] = "El enlace para restablecer la contraseña no es válido.";

                return RedirectToAction(nameof(ForgotPassword));
            }

            ResetPasswordRequestViewModel vm = new()
            {
                Id = userId,
                Token = token,
                Password = string.Empty,
                ConfirmPassword = string.Empty
            };

            return View(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            ResetPasswordRequestDto dto = new()
            {
                Id = vm.Id,
                Token = vm.Token,
                Password = vm.Password
            };

            UserResponseDto result = await _accountServiceForWebApp.ResetPasswordAsync(dto);

            if (result.HasError)
            {
                foreach (string error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                vm.Password = string.Empty;
                vm.ConfirmPassword = string.Empty;

                return View(vm);
            }

            TempData["PasswordResetSuccess"] = "Su contraseña fue restablecida correctamente. " +
                "Ya puede iniciar sesión.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegistrationResult()
        {
            if (TempData["RegistrationEmailSent"] is not string emailSent)
            {
                return RedirectToAction("Index", "Login");
            }

            return View(bool.Parse(emailSent));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;

            return View();
        }
    }    
}