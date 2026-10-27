using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.FriendshipRequest
{
    public class NewFriendshipRequestViewModel
    {
        public string? UserNameFilter { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un usuario para enviar la solicitud de amistad.")]
        public string? SelectedUserId { get; set; }

        public List<AvailableFriendshipUserViewModel> Users { get; set; } = [];
    }
}
