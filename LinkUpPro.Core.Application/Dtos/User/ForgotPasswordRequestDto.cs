namespace LinkUpPro.Core.Application.Dtos.User
{
    public class ForgotPasswordRequestDto
    {
        public required string UserName { get; set; }
        public required string Origin { get; set; }
    }
}
