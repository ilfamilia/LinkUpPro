using LinkUpPro.Core.Application.Dtos.User;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IAccountServiceForWebApp
    {
        Task<LoginResponseDto> AuthenticateAsync(LoginDto loginDto);
        Task<string> ConfirmAccountAsync(string userId, string token);
        Task<EditResponseDto> EditUserAsync(EditUserDto saveDto, string origin,bool? isCreated = false);
        Task<List<UserDto>> GetAllUserAsync(bool? isActive = true);
        Task<UserDto?> GetUserByUserNameAsync(string userName);
        Task<UserDto?> GetUserByIdAsync(string Id);
        Task<RegisterResponseDto> RegisterUserAsync(RegisterUserDto saveDto, string origin);
        Task<UserResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<UserResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task SignOutAsync();
        Task<string> ResendEmailActivationAsync(EmailConfirmationDto request, string origin);
    }
}