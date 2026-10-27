using System.ComponentModel.DataAnnotations;
namespace LinkUpPro.Core.Application.ViewModels.User
{
    public class EmailConfirmationViewModel
    {
        [Required(ErrorMessage = "Debe ingresar el nombre del usuario.")]
        [DataType(DataType.Text)]
        public required string UserName { get; set; }
    }    
}
