using LinkUpPro.Core.Application.ViewModels.Post;

namespace LinkUpPro.Core.Application.ViewModels.Friendship
{
    public class FriendshipIndexViewModel
    {
        public FriendshipSummaryViewModel Summary { get; set; } = new();
        public FriendshipPostFilterViewModel Filter { get; set; } = new();
        public string? FriendSearch { get; set; }
        public List<FriendViewModel> Friends { get; set; } = [];
        public List<FriendViewModel> FilteredFriends { get; set; } = [];
        public List<PostViewModel> Posts { get; set; } = [];
    }
}
