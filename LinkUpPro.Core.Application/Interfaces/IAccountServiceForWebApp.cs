using LinkUpPro.Core.Application.Dtos.User;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IAccountServiceForWebApp
    {
        Task<LoginResponseDto> AuthenticateAsync(LoginDto loginDto);
        Task<string> ConfirmAccountAsync(string userId, string token);
        Task<EditResponseDto> EditUser(EditUserDto saveDto, string origin,bool? isCreated = false);
        Task<List<UserDto>> GetAllUser(bool? isActive = true);
        Task<UserDto?> GetUserByUserName(string userName);
        Task<UserDto?> GetUserById(string Id);
        Task<RegisterResponseDto> RegisterUser(SaveUserDto saveDto, string origin);
        Task<UserResponseDto> ForgotPassword(ForgotPasswordRequestDto request);
        Task<UserResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task SignOutAsync();
        Task<string> ResendEmailActivation(LoginDto loginDto, string origin);
    }
}