namespace LinkUpPro.Core.Application.Dtos.User
{
    public class EditResponseDto
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public required string PhoneNumber { get; set; }
        public string? ProfileImage { get; set; }
        public required string CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public required string ConfirmPassword { get; set; }
        public bool HasError { get; set; }
        public required List<string> Errors { get; set; }
    }
}
