using LinkUpPro.Core.Application.Dtos.User;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IAccountServiceForWebApp
    {
        Task<LoginResponseDto> AuthenticateAsync(LoginDto loginDto);
        Task SignOutAsync();
        Task<RegisterResponseDto> RegisterUserAsync(RegisterUserDto saveDto, string origin);
        Task<UserResponseDto> ConfirmAccountAsync(string userId, string token);
        Task<UserResponseDto> ResendEmailActivationAsync(EmailConfirmationDto request, string origin);
        Task<UserResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<UserResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task<UserResponseDto> ValidateResetTokenAsync(string userId, string token);
        Task<EditResponseDto> EditUserAsync(EditUserDto saveDto);
        Task<List<UserDto>> GetAllUserAsync(bool? isActive = true);
        Task<UserDto?> GetUserByUserNameAsync(string userName);
        Task<UserDto?> GetUserByIdAsync(string Id);
        Task<bool> IsUserNameTakenAsync(string userName);
        Task<bool> IsEmailTakenAsync(string email);
    }
}
