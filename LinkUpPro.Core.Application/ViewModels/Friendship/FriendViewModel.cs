namespace LinkUpPro.Core.Application.ViewModels.Friendship
{
    public class FriendViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? ProfileImage { get; set; }
        public int MutualFriendsCount { get; set; }
    }
}
