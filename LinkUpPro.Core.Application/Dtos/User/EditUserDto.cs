namespace LinkUpPro.Core.Application.Dtos.User
{
    public class EditUserDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string Phone { get; set; }
        // New stored photo path; null keeps the current photo.
        public string? ProfileImage { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}
