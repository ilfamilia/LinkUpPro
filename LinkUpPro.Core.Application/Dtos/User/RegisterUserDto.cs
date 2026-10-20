namespace LinkUpPro.Core.Application.Dtos.User
{
    public class RegisterUserDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public required string Phone { get; set; }
        public string? ProfileImage { get; set; }
    }
}
