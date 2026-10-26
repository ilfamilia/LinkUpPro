using System.ComponentModel.DataAnnotations;

namespace LinkUpPro.Core.Application.ViewModels.Friendship
{
    public class FriendshipIndexQueryViewModel
    {
        [StringLength(100, ErrorMessage = "La búsqueda de amigos no puede superar 100 caracteres.")]
        public string? FriendSearch { get; set; }
        public FriendshipPostFilterViewModel Filter { get; set; } = new();
    }
}
