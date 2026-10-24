namespace LinkUpPro.Core.Application.Dtos.User
{
    public class EditUserDto //a
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string Phone { get; set; }
        public string? ProfileImage { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }   //a
}