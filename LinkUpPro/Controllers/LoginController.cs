using AutoMapper;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.ViewModels;
using LinkUpPro.Core.Application.Services;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using LinkUpPro.Core.Application.ViewModels.User;

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

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel vm)
        {
            AppUser? userSession = await _userManager.GetUserAsync(User);

            if(userSession != null)
            {
                return RedirectToAction("Index", "Home");
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
                foreach(var error in LoginResponse?.Errors ?? [])
                {
                    ModelState.AddModelError("userValidation", error);
                }    
            }
            vm.Password = "";
            return View(vm);
        }

        public async Task<ActionResult> Logout()
        {
            await _accountServiceForWebApp.SignOutAsync();
            return RedirectToAction("Index", "Login");
        }
        public IActionResult Register()
        {
            return View(new RegisterUserViewModel()
            {
                Name = "",
                LastName = "",
                Phone = "",
                Email = "",
                UserName = "",
                Password = "",
                ConfirmPassword = "",
                ProfileImageFile = null
            });
        }

        [HttpPost]
        public async Task<ActionResult> Register(RegisterUserViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }
    
            RegisterUserDto registerUserDto = _mapper.Map<RegisterUserDto>(vm);
            string origin = Request?.Headers?.Origin.ToString() ?? string.Empty;

            RegisterResponseDto? registerUserResponse = await _accountServiceForWebApp.RegisterUserAsync(registerUserDto, origin);

            if (registerUserResponse.HasError)
            {
                ViewBag.HasError = true;
                ViewBag.Errors = registerUserResponse.Errors;
                return View(vm);
            }
            return RedirectToRoute(new { controller = "Login", action = "Index" });
        }
        
        public async Task<ActionResult> ResendEmailActivation()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<ActionResult> ResendEmailActivation(EmailConfirmationViewModel vm)
        {
            EmailConfirmationDto dto = new() { UserName = vm.UserName };
            string origin = Request?.Headers?.Origin.ToString() ?? string.Empty;
            await _accountServiceForWebApp.ResendEmailActivationAsync(dto, origin);
            return View();
        }

        public async Task<IActionResult> ConfirmEmail(string userId,string token)
        {
            string response = await _accountServiceForWebApp.ConfirmAccountAsync(userId, token);
            return View("ConfirmEmail", response);
        }

        public IActionResult ForgotPassword()
        {           
            return View(new ForgotPasswordRequestViewModel() {UserName = ""});
        } 

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestViewModel vm)
        {
            if(!ModelState.IsValid)
            {
                return View(vm);
            }
            string origin = Request?.Headers?.Origin.ToString() ?? string.Empty;
            ForgotPasswordRequestDto Dto = new() {UserName = vm.UserName, Origin = origin};
            UserResponseDto? returnUser = await _accountServiceForWebApp.ForgotPasswordAsync(Dto);

            if (returnUser.HasError)
            {
                ViewBag.HasError = true;
                ViewBag.Errors = returnUser.Errors;
                return View(vm);
            }
            return RedirectToRoute(new { controller = "Login", action = "Index" });
        }

        public IActionResult ResetPassword(string userId, string token)
        {           
            return View(new ResetPasswordRequestViewModel() { Id = userId,Token = token,Password = ""});
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }  

            ResetPasswordRequestDto dto = new() { Id = vm.Id ,Password = vm.Password,Token = vm.Token };
            UserResponseDto? returnUser = await _accountServiceForWebApp.ResetPasswordAsync(dto);

            if (returnUser.HasError)
            {
                ViewBag.HasError = true;
                ViewBag.Errors = returnUser.Errors;
                return View(vm);
            }

            return RedirectToRoute(new { controller = "Login", action = "Index" });
        }
    }    
}
