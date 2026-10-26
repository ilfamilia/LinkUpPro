using System.ComponentModel.DataAnnotations;
using LinkUpPro.Core.Application.ViewModels.Post;

namespace LinkUpPro.Core.Application.ViewModels.Friendship
{
    public class FriendshipPostFilterViewModel : PostFilterViewModel
    {
        [StringLength(36, ErrorMessage = "El amigo seleccionado no es válido.")]
        public string? FriendUserId { get; set; }
    }
}
