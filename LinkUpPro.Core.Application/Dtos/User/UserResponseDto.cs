namespace LinkUpPro.Core.Application.Dtos.User
{
    public class UserResponseDto
    {
        public bool HasError { get; set; }
        public required List<string> Errors { get; set; }
        // Message shown to the user when the operation finishes (success or generic notice).
        public string? Message { get; set; }
    }
}
