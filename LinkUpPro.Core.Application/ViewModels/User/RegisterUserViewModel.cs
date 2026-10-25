using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace LinkUpPro.Core.Application.ViewModels.User
{
    public class RegisterUserViewModel
    {
        [Required(ErrorMessage = "Debe ingresar su nombre.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Debe ingresar su apellido.")]
        [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Debe ingresar su teléfono.")]
        [RegularExpression(AccountValidation.PhonePattern, ErrorMessage = AccountValidation.PhoneMessage)]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Debe ingresar su correo electrónico.")]
        [EmailAddress(ErrorMessage = "Debe ingresar un correo electrónico válido.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Debe seleccionar una foto de perfil.")]
        [DataType(DataType.Upload)]
        public IFormFile? ProfileImageFile { get; set; }

        [Required(ErrorMessage = "Debe ingresar un nombre de usuario.")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede superar los 50 caracteres.")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Debe ingresar una contraseña.")]
        [RegularExpression(AccountValidation.PasswordPattern, ErrorMessage = AccountValidation.PasswordMessage)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [Compare(nameof(Password), ErrorMessage = "La contraseña y su confirmación no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }
}
