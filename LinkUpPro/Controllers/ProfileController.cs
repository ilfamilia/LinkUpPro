using AutoMapper;
using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.ViewModels.User;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers;

/// <summary>"Mi perfil": the signed-in user edits only their own account; the id always comes from the session.</summary>
public sealed class ProfileController(IAccountServiceForWebApp accounts, ICurrentUserService currentUser,
    IImageStorageService imageStorage, IMapper mapper)
    : SocialController
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new EditUserViewModel();
        if (!await LoadAccountDataAsync(vm, overwriteEditableFields: true)) return Challenge();
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Index(EditUserViewModel vm)
    {
        // Read-only values (email, username, photo) are reloaded from the server, never taken from the form.
        if (!await LoadAccountDataAsync(vm, overwriteEditableFields: false)) return Challenge();

        bool anyPassword = !string.IsNullOrEmpty(vm.CurrentPassword) || !string.IsNullOrEmpty(vm.NewPassword)
            || !string.IsNullOrEmpty(vm.ConfirmPassword);
        bool allPasswords = !string.IsNullOrEmpty(vm.CurrentPassword) && !string.IsNullOrEmpty(vm.NewPassword)
            && !string.IsNullOrEmpty(vm.ConfirmPassword);

        if (anyPassword && !allPasswords)
        {
            ModelState.AddModelError(string.Empty,
                "Para cambiar su contraseña debe completar la contraseña actual, la nueva contraseña y su confirmación.");
        }

        if (!ModelState.IsValid) return ProfileView(vm);

        string userId = currentUser.GetRequiredUserId();
        string? newImagePath = null;

        if (vm.ProfileImageFile is not null)
        {
            try
            {
                newImagePath = await imageStorage.SaveAsync(vm.ProfileImageFile, userId, "Users");
            }
            catch (InvalidDataException ex)
            {
                ModelState.AddModelError(nameof(vm.ProfileImageFile), ex.Message);
                return ProfileView(vm);
            }
            catch (IOException)
            {
                ModelState.AddModelError(nameof(vm.ProfileImageFile), "No fue posible actualizar la foto de perfil. Inténtelo nuevamente.");
                return ProfileView(vm);
            }
        }

        EditUserDto dto = mapper.Map<EditUserDto>(vm);
        dto.Id = userId;
        dto.ProfileImage = newImagePath;

        EditResponseDto response = await accounts.EditUserAsync(dto);

        if (response.HasError)
        {
            // Nothing was saved: drop the new photo and keep the previous one.
            imageStorage.Delete(newImagePath);
            foreach (string error in response.Errors) ModelState.AddModelError(string.Empty, error);
            return ProfileView(vm);
        }

        // The old photo is removed only after the new one is stored and linked to the profile.
        if (newImagePath is not null && response.PreviousProfileImage != newImagePath)
        {
            imageStorage.Delete(response.PreviousProfileImage);
        }

        if (response.PasswordChanged)
        {
            await accounts.SignOutAsync();
            TempData["LoginSuccess"] = "Su perfil y contraseña fueron actualizados correctamente. Inicie sesión nuevamente.";
            return RedirectToAction("Index", "Login");
        }

        TempData["Success"] = "Su perfil fue actualizado correctamente.";
        return RedirectToAction("Index", "Home");
    }

    private async Task<bool> LoadAccountDataAsync(EditUserViewModel vm, bool overwriteEditableFields)
    {
        var user = await accounts.GetUserByIdAsync(currentUser.GetRequiredUserId());
        if (user is null) return false;

        vm.Email = user.Email;
        vm.UserName = user.UserName;
        vm.ProfileImagePath = user.ProfileImage;

        if (overwriteEditableFields)
        {
            vm.Name = user.Name;
            vm.LastName = user.LastName;
            vm.Phone = user.Phone ?? "";
        }

        return true;
    }

    // Password inputs never render their value, so they always come back empty; errors stay visible.
    private IActionResult ProfileView(EditUserViewModel vm)
    {
        vm.CurrentPassword = null;
        vm.NewPassword = null;
        vm.ConfirmPassword = null;
        return View("Index", vm);
    }
}
