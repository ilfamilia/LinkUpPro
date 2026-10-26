using LinkUpPro.Core.Application.ViewModels.Post;

namespace LinkUpPro.Core.Application.ViewModels.Friendship
{
    public class FriendshipProfileViewModel
    {
        public FriendViewModel Friend { get; set; } = new();
        public List<PostViewModel> Posts { get; set; } = [];
    }
}
