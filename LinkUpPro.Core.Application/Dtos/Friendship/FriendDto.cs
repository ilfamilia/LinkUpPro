namespace LinkUpPro.Core.Application.Dtos.Friendship
{
    public class FriendDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string UserName { get; set; }
        public string? ProfileImage { get; set; }
        public int MutualFriendsCount { get; set; }
    }
}
