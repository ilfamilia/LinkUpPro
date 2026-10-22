using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LinkUpPro.Controllers
{
    [AllowAnonymous]
    [Route("Login")]
    public class DevLoginController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        public DevLoginController(UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Index(string? returnUrl)
        {
            if (!IsDevelopmentAccessAllowed())
            {
                return NotFound();
            }

            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enter(string? returnUrl)
        {
            if (!IsDevelopmentAccessAllowed())
            {
                return NotFound();
            }

            const string email = "dev-posts@linkuppro.test";

            AppUser? user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = "Usuario",
                    LastName = "Prueba",
                    ProfilePicturePath = "",
                    CreatedAt = DateTime.UtcNow
                };

                IdentityResult result = await _userManager.CreateAsync(user);

                if (!result.Succeeded)
                {
                    return Problem(string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        private bool IsDevelopmentAccessAllowed()
        {
            IPAddress? address = HttpContext.Connection.RemoteIpAddress;

            return _environment.IsDevelopment() && address != null &&
                   IPAddress.IsLoopback(address);
        }
    }
}