using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.User
{
    public class RegisterUserViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [RegularExpression(@".*\S.*", ErrorMessage = "El nombre no puede contener únicamente espacios.")]
        [DataType(DataType.Text)]
        public required string Name { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [RegularExpression(@".*\S.*", ErrorMessage = "El apellido no puede contener únicamente espacios.")]
        [DataType(DataType.Text)]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^(?:\+1[ -]?)?(?:809|829|849)[ -]?[0-9]{3}[ -]?[0-9]{4}$",
            ErrorMessage = "Ingrese un número telefónico válido de República Dominicana.")]
        [DataType(DataType.PhoneNumber)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [DataType(DataType.EmailAddress)]
        public required string Email { get; set; }

        [Required(ErrorMessage = "La fotografía de perfil es obligatoria.")]
        [DataType(DataType.Upload)]
        public required IFormFile ProfileImageFile { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [DataType(DataType.Text)]
        public required string UserName { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).{8,}$",
            ErrorMessage = "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial.")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [Compare(nameof(Password),
            ErrorMessage = "La contraseña y su confirmación no coinciden.")]
        [DataType(DataType.Password)]
        public required string ConfirmPassword { get; set; }
    }
}