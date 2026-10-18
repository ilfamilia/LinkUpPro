using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace InvestmentApp.Core.Application.ViewModels.User
{
    public class UpdateUserViewModel
    {
        public required string Id { get; set; }

        [Required(ErrorMessage = "You must enter the name of user")]
        [DataType(DataType.Text)]
        public required string Name { get; set; }

        [Required(ErrorMessage = "You must enter the last name of user")]
        [DataType(DataType.Text)]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "You must enter the phone of user")]
        [DataType(DataType.Text)]
        public required string Phone { get; set; }

        [Required(ErrorMessage = "You must upload a profile image")]
        [DataType(DataType.Upload)]
        public IFormFile? ProfileImageFile { get; set; }

        public required string CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Compare(nameof(Password), ErrorMessage = "Password must match")]       
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }



    }
}
