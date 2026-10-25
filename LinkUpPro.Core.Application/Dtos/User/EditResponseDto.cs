namespace LinkUpPro.Core.Application.Dtos.User
{
    public class EditResponseDto
    {
        public bool HasError { get; set; }
        public required List<string> Errors { get; set; }
        // True when the password was changed and every previous session was invalidated.
        public bool PasswordChanged { get; set; }
        // Photo path stored before this update, so the caller can delete it once the change is saved.
        public string? PreviousProfileImage { get; set; }
    }
}
