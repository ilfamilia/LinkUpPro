using System.ComponentModel.DataAnnotations;
namespace LinkUpPro.Core.Application.ViewModels.User
{
    public class ResetPasswordRequestViewModel
    {
        [Required]
        public string Id { get; set; } = "";

        [Required]
        public string Token { get; set; } = "";

        [Required(ErrorMessage = "Debe ingresar la nueva contraseña.")]
        [RegularExpression(AccountValidation.PasswordPattern, ErrorMessage = AccountValidation.PasswordMessage)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Debe confirmar la nueva contraseña.")]
        [Compare(nameof(Password), ErrorMessage = "La contraseña y su confirmación no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }
}
