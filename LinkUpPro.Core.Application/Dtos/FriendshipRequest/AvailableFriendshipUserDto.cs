namespace LinkUpPro.Core.Application.Dtos.FriendshipRequest
{
    public class AvailableFriendshipUserDto
    {
        public required FriendshipUserDto User { get; set; }
        public int MutualFriendsCount { get; set; }
    }
}
