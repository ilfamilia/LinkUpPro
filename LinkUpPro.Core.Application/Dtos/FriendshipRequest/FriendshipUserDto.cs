namespace LinkUpPro.Core.Application.Dtos.FriendshipRequest
{
    public class FriendshipUserDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string UserName { get; set; }
        public string? ProfileImage { get; set; }
    }
}
