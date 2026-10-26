namespace LinkUpPro.Core.Application.ViewModels.Friendship
{
    public class FriendshipMutualFriendsViewModel
    {
        public FriendViewModel Friend { get; set; } = new();
        public List<FriendViewModel> MutualFriends { get; set; } = [];
    }
}
