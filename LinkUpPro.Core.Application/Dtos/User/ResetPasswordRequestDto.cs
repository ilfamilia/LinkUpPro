namespace LinkUpPro.Core.Application.Dtos.User
{
    public class ResetPasswordRequestDto
    {
        public required string Id { get; set; }

        public required string Token { get; set; }

        public required string Password { get; set; }    
    }
}
