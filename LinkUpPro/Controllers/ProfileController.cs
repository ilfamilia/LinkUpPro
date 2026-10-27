using AutoMapper;
using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.ViewModels.User;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IAccountServiceForWebApp _accountService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public ProfileController(IAccountServiceForWebApp accountService,
            UserManager<AppUser> userManager, IMapper mapper)
        {
            _accountService = accountService;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            AppUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            if (!user.EmailConfirmed)
            {
                return Forbid();
            }

            UserDto? userDto = await _accountService.GetUserByIdAsync(user.Id);

            if (userDto == null)
            {
                return NotFound();
            }

            EditUserViewModel vm = _mapper.Map<EditUserViewModel>(userDto);

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel vm)
        {
            AppUser? user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            if (!user.EmailConfirmed)
            {
                return Forbid();
            }

            vm.UserName = user.UserName ?? string.Empty;
            vm.Email = user.Email ?? string.Empty;
            vm.ProfileImagePath = user.ProfilePicturePath;

            if (!ModelState.IsValid)
            {
                return View("Index", vm);
            }

            EditUserDto dto = _mapper.Map<EditUserDto>(vm);

            dto.Id = user.Id;
            dto.Name = vm.Name.Trim();
            dto.LastName = vm.LastName.Trim();
            dto.Phone = vm.Phone.Trim();

            string? oldImagePath = user.ProfilePicturePath;
            string? newImagePath = null;

            bool updateSucceeded = false;

            try
            {
                if (vm.ProfileImageFile != null)
                {
                    try
                    {
                        newImagePath = LinkUpPro.Helpers.FileManager.Upload(vm.ProfileImageFile,
                            user.Id, "Users");
                    }
                    catch (InvalidDataException ex)
                    {
                        ModelState.AddModelError(nameof(vm.ProfileImageFile), ex.Message);

                        return View("Index", vm);
                    }
                    catch (IOException)
                    {
                        ModelState.AddModelError(nameof(vm.ProfileImageFile),
                            "No fue posible actualizar la foto de perfil.");

                        return View("Index", vm);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        ModelState.AddModelError(nameof(vm.ProfileImageFile),
                            "No fue posible actualizar la foto de perfil.");

                        return View("Index", vm);
                    }
                }

                dto.ProfileImage = newImagePath;

                string origin = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

                EditResponseDto result = await _accountService.EditUserAsync(dto, origin);

                if (result.HasError)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error);
                    }

                    vm.CurrentPassword = null;
                    vm.NewPassword = null;
                    vm.ConfirmPassword = null;

                    ModelState.Remove(nameof(vm.CurrentPassword));
                    ModelState.Remove(nameof(vm.NewPassword));
                    ModelState.Remove(nameof(vm.ConfirmPassword));

                    return View("Index", vm);
                }

                updateSucceeded = true;
            }
            finally
            {
                if (!updateSucceeded && !string.IsNullOrWhiteSpace(newImagePath))
                {
                    try
                    {
                        LinkUpPro.Helpers.FileManager.DeleteFile(newImagePath);
                    }
                    catch (IOException)
                    {
                        // No ocultar el error de la actualización.
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // No ocultar el error de la actualización.
                    }
                }
            }

            string userImageDirectory = $"Images/Users/{user.Id}/";

            if (!string.IsNullOrWhiteSpace(newImagePath) && !string.IsNullOrWhiteSpace(oldImagePath) &&
                oldImagePath.StartsWith(userImageDirectory, StringComparison.OrdinalIgnoreCase) &&
                oldImagePath != newImagePath)
            {
                try
                {
                    LinkUpPro.Helpers.FileManager.DeleteFile(oldImagePath);
                }
                catch (IOException)
                {
                    // Los cambios de Identity ya fueron guardados.
                }
                catch (UnauthorizedAccessException)
                {
                    // Los cambios de Identity ya fueron guardados.
                }
            }

            bool passwordChanged = !string.IsNullOrWhiteSpace(vm.NewPassword);

            if (passwordChanged)
            {
                await _accountService.SignOutAsync();

                TempData["ProfilePasswordChanged"] = "Su perfil y contraseña fueron actualizados correctamente. " +
                    "Inicie sesión nuevamente.";

                return RedirectToAction("Index", "Login");
            }

            TempData["ProfileUpdated"] = "Su perfil fue actualizado correctamente.";

            return RedirectToAction("Index", "Home");
        }
    }
}