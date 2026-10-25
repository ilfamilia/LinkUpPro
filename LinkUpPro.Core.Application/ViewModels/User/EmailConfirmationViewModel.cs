using System.ComponentModel.DataAnnotations;
namespace LinkUpPro.Core.Application.ViewModels.User
{
    public class EmailConfirmationViewModel
    {
        [Required(ErrorMessage = "Debe ingresar su nombre de usuario.")]
        public string UserName { get; set; } = "";
    }
}
