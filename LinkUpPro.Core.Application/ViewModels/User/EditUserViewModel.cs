using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.User
{
    public class EditUserViewModel
    {
        [Required(ErrorMessage = "Debe ingresar su nombre")]
        [RegularExpression(@".*\S.*", ErrorMessage = "El nombre no puede contener únicamente espacios")]
        [DataType(DataType.Text)]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Debe ingresar su apellido.")]
        [RegularExpression(@".*\S.*", ErrorMessage = "El apellido no puede contener únicamente espacios.")]
        [DataType(DataType.Text)]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "Debe ingresar su teléfono.")]
        [RegularExpression(@"^(?:\+1[ -]?)?(?:809|829|849)[ -]?[0-9]{3}[ -]?[0-9]{4}$",
            ErrorMessage = "Debe ingresar un número telefónico válido de República Dominicana.")]
        [DataType(DataType.PhoneNumber)]
        public required string Phone { get; set; }

        [DataType(DataType.Upload)]
        public IFormFile? ProfileImageFile { get; set; }

        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).{8,}$",
            ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial.")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? ProfileImagePath { get; set; }
    }
}