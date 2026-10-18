using LinkUpPro.Core.Application.Dtos.User;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IAccountServiceForWebApp
    {
        Task<LoginResponseDto> AuthenticateAsync(LoginDto loginDto);
        Task<string> ConfirmAccountAsync(string userId, string token);
        Task<UserResponseDto> DeleteAsync(string id);
        Task<EditResponseDto> EditUser(SaveUserDto saveDto, string origin,bool? isCreated = false);
        Task<UserResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<List<UserDto>> GetAllUser(bool? isActive = true);
        Task<UserDto?> GetUserByEmail(string email);
        Task<UserDto?> GetUserByUserName(string userName);
        Task<UserDto?> GetUserById(string Id);
        Task<RegisterResponseDto> RegisterUser(SaveUserDto saveDto, string origin);
        Task<UserResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
        Task SignOutAsync();
    }
}