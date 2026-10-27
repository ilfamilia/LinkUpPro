using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Friendship
{
    public class FriendshipRemoveViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un amigo.")]
        public string FriendUserId { get; set; } = string.Empty;
        
        public string UserName { get; set; } = string.Empty;
    }
}
