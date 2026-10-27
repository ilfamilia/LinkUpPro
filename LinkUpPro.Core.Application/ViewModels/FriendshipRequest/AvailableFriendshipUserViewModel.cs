namespace LinkUpPro.Core.Application.ViewModels.FriendshipRequest
{
    public class AvailableFriendshipUserViewModel
    {
        public FriendshipUserViewModel User { get; set; } = new();
        public int MutualFriendsCount { get; set; }
    }
}