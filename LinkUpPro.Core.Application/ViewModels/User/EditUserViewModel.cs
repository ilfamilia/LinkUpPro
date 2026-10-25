using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LinkUpPro.Core.Application.ViewModels.User
{
    // "Mi perfil" form. The user id is never bound: the server always edits the authenticated account.
    public class EditUserViewModel
    {
        [Required(ErrorMessage = "Debe ingresar su nombre.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Debe ingresar su apellido.")]
        [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Debe ingresar un número telefónico válido de República Dominicana.")]
        [RegularExpression(AccountValidation.PhonePattern, ErrorMessage = AccountValidation.PhoneMessage)]
        public string Phone { get; set; } = "";

        [DataType(DataType.Upload)]
        public IFormFile? ProfileImageFile { get; set; }

        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [RegularExpression(AccountValidation.PasswordPattern, ErrorMessage = AccountValidation.PasswordMessage)]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Compare(nameof(NewPassword), ErrorMessage = "La nueva contraseña y su confirmación no coinciden.")]
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }

        // Read-only data shown on the page; the controller always reloads it from the server.
        public string Email { get; set; } = "";

        public string UserName { get; set; } = "";

        public string? ProfileImagePath { get; set; }
    }
}
