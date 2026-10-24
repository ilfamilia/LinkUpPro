using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using LinkUpPro.Core.Application.Dtos.Email;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace LinkUpPro.Infrastructure.Identity.Services
{
    public class AccountServiceForWebApp : IAccountServiceForWebApp
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailService _emailService;
        //a
        public AccountServiceForWebApp(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }
        public async Task<LoginResponseDto> AuthenticateAsync(LoginDto loginDto)
        {
            LoginResponseDto response = new()
            {
                Email = "",
                Id = "",
                LastName = "",
                Name = "",
                UserName = "",
                HasError = false,
                Errors = []
            }; //a

            var user = await _userManager.FindByNameAsync(loginDto.UserName);

            if (user == null)
            {
                response.HasError = true;
                response.Errors.Add($"There is no acccount registered with this username: {loginDto.UserName}");
                return response;
            }

            if (!user.EmailConfirmed)
            {
                response.HasError = true;
                response.Errors.Add($"This account {loginDto.UserName} is not active, you should check your email");
                return response;
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName ?? "", loginDto.Password, loginDto.RememberMe, true);

            if (!result.Succeeded)
            {
                response.HasError = true;
                if (result.IsLockedOut)
                {
                    response.Errors.Add($"Your account {loginDto.UserName} has been locked due to multiple failed attempts." +
                        $" Please try again in 15 minutes. If you don’t remember your password, you can go through the password " +
                        $"reset process.");
                }
                else
                {
                    response.Errors.Add($"these credentials are invalid for this user: {user.UserName}");
                }                    
                return response;
            }

            response.Id = user.Id;
            response.Email = user.Email ?? "";
            response.UserName = user.UserName ?? "";
            response.Name = user.FirstName ?? "";
            response.LastName = user.LastName;
            response.IsVerified = user.EmailConfirmed;

            return response;
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }
        
        public async Task<RegisterResponseDto> RegisterUserAsync(RegisterUserDto saveDto, string origin)
        {
            RegisterResponseDto RegisterRes = new(){
                Email = "",
                Id = "",
                LastName = "",
                Name = "",
                UserName = "",
                HasError = false,
                Errors = []
            };

            var userWithSameEmailName = await _userManager.FindByEmailAsync(saveDto.Email);
            if (userWithSameEmailName != null)
            {
                RegisterRes.HasError = true;
                RegisterRes.Errors.Add($"This email is already registered: {saveDto.Email}");
                return RegisterRes;
            }

            var userWithSameUserName = await _userManager.FindByNameAsync(saveDto.UserName);
            if (userWithSameUserName != null)
            {
                RegisterRes.HasError = true;
                RegisterRes.Errors.Add($"This username is already registered: {saveDto.UserName}");
                return RegisterRes;
            }

            AppUser user = new AppUser()
            {
                FirstName = saveDto.Name,
                LastName = saveDto.LastName,
                Email = saveDto.Email,
                UserName = saveDto.UserName,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                ProfilePicturePath = saveDto.ProfileImage ?? ""
            };

            var result = await _userManager.CreateAsync(user, saveDto.Password);

            if (result.Succeeded)
            {
                string verificationUri = await GetVerificationEmailUriAsync(user, origin);
                await _emailService.SendEmailAsync(new EmailRequestDto()
                {
                    To = user.Email,
                    Subject = "Confirm your email",
                    HtmlBody = $"Please confirm your account by clicking this link: <a href='{verificationUri}'>link</a>"
                });
                RegisterRes.Id = user.Id;
                RegisterRes.Email = user.Email ?? "";
                RegisterRes.UserName = user.UserName ?? "";
                RegisterRes.Name = user.FirstName ?? "";
                RegisterRes.LastName = user.LastName ?? "";
                RegisterRes.IsVerified = user.EmailConfirmed;
                RegisterRes.HasError = false;

                return RegisterRes;
            }
            else
            {
                RegisterRes.HasError = true;
                RegisterRes.Errors.AddRange(result.Errors.Select(s => s.Description).ToList());
                return RegisterRes;
            }
        }
        
        public async Task<string> ConfirmAccountAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return "No existe cuenta registrada con este usuario";
            }

            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return $"Cuenta confirmada para {user.Email}. Ahora puede iniciar sesión.";
            }
            else
            {
                return $"Ocurrio un error al confirmar el correo {user.Email}";
            }
        }

        public async Task<string> ResendEmailActivationAsync(EmailConfirmationDto request,string origin)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                return $"No existe cuenta registrada con este usuario";
            }

            if (user.EmailConfirmed)
            {
                return $"Esta cuenta {request.UserName} ya ha sido activada, por favor inicie sesión";
            }
            
            string verificationUri = await GetVerificationEmailUriAsync(user, origin);
            await _emailService.SendEmailAsync(new EmailRequestDto()
            {
                To = user.Email,
                Subject = "Confirm your email",
                HtmlBody = $"Please confirm your account by clicking this link: <a href='{verificationUri}'>link</a>"
            });
            
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            return "The email confirmation has been resent. Please check your email to activate your account.";
        }

        public async Task<List<UserDto>> GetAllUserAsync(bool? isActive = true)
        {
            List<UserDto> ListUsersDto = [];

            var users = _userManager.Users;
            
            if(isActive != null && isActive == true)
            {
                users = users.Where(u => u.EmailConfirmed);
            }

            var listUsers = await users.ToListAsync();
            
            foreach (var item in listUsers)
            {
                ListUsersDto.Add(new UserDto()
                {
                    Id = item.Id,
                    Name = item.FirstName ?? "",
                    LastName = item.LastName ?? "",
                    Email = item.Email ?? "",
                    UserName = item.UserName ?? "",
                    Phone = item.PhoneNumber,
                    ProfileImage = item.ProfilePicturePath,
                    isVerified = item.EmailConfirmed
                });
            }
            return ListUsersDto;
        }

        public async Task<UserDto?> GetUserByUserNameAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if(user == null)
            {
                return null;
            }
            UserDto userDto = new UserDto()
            {
                Id = user.Id,
                Name = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                Phone = user.PhoneNumber,
                ProfileImage = user.ProfilePicturePath,
                isVerified = user.EmailConfirmed
            };
            return userDto;
        }

        public async Task<UserDto?> GetUserByIdAsync(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            if(user == null)
            {
                return null;
            }
            UserDto userDto = new UserDto()
            {
                Id = user.Id,
                Name = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                Phone = user.PhoneNumber,
                ProfileImage = user.ProfilePicturePath,
                isVerified = user.EmailConfirmed
            };
            return userDto;
        }

        public async Task<EditResponseDto> EditUserAsync(EditUserDto EditUserDto, string origin, bool? isCreated = false)
        {
            EditResponseDto Response = new(){ //a
                Id = "",
                Name = "",
                LastName = "",
                Phone = "",
                ProfileImage = "",
                CurrentPassword = "",
                NewPassword = "",
                HasError = false,
                Errors = []
            };

            var user = await _userManager.FindByIdAsync(EditUserDto.Id);

            if (user == null)
            {
                Response.HasError = true;
                Response.Errors.Add($"There is no acccount registered with this user");
                return Response;
            }

            user.FirstName = EditUserDto.Name;
            user.LastName = EditUserDto.LastName;
            user.PhoneNumber = EditUserDto.Phone;
            user.ProfilePicturePath = string.IsNullOrWhiteSpace(EditUserDto.ProfileImage) ? user.ProfilePicturePath : EditUserDto.ProfileImage;

            if (!string.IsNullOrWhiteSpace(EditUserDto.CurrentPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resultChange = await _userManager.ResetPasswordAsync(user, token, EditUserDto.NewPassword ?? "");

                if (resultChange != null && !resultChange.Succeeded)
                {
                    Response.HasError = true;
                    Response.Errors.AddRange(resultChange.Errors.Select(s => s.Description).ToList());
                    return Response;
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return new EditResponseDto()
                {
                    Id = user.Id,
                    Name = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    Phone = user.PhoneNumber ?? "",
                    ProfileImage = user.ProfilePicturePath,
                    CurrentPassword = EditUserDto.CurrentPassword ?? "",
                    NewPassword = EditUserDto.NewPassword ?? "",
                    HasError = !result.Succeeded,
                    Errors = result.Errors.Select(s => s.Description).ToList()
                };
            }
            else
            {
                Response.HasError = true;
                Response.Errors.AddRange(result.Errors.Select(s => s.Description).ToList());
                return Response;
            }
        }

        public async Task<UserResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            UserResponseDto response = new() { HasError = false, Errors = [] };

            var user = await _userManager.FindByEmailAsync(request.UserName);

            if(user == null)
            {
                response.HasError = true;
                response.Errors.Add($"There is no acccount registered with this email: {request.UserName}");
                return response;
            }

            var ResetPasswordUri = await GetResetPasswordUriAsync(user, request.Origin);

            await _emailService.SendEmailAsync(new EmailRequestDto()
            {
               To = user.Email,
               Subject = "Reset Password",
               HtmlBody = $"Please reset your password by clicking this link: <a href='{ResetPasswordUri}'>link</a>" 
            });
            return response;
        }

        public async Task<UserResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            UserResponseDto response = new() { HasError = false, Errors = [] };

            var user = await _userManager.FindByIdAsync(request.Id);

            if(user == null)
            {
                response.HasError = true;
                response.Errors.Add($"There is no acccount registered with this user");
                return response;
            }
            var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            var result = await _userManager.ResetPasswordAsync(user, token, request.Password);

            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Errors.Add($"An error occurred while resetting the password for this user");
                return response;
            }

            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            return response;
        }

        private async Task<string> GetVerificationEmailUriAsync(AppUser user, string origin)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var route = "Login/ConfirmEmail";
            var completeUrl = new Uri(string.Concat(origin, "/", route));
            var verificationUri = QueryHelpers.AddQueryString(completeUrl.ToString(), "userId", user.Id);
            verificationUri = QueryHelpers.AddQueryString(verificationUri.ToString(), "token", token);
            return verificationUri;
        }

        private async Task<string> GetResetPasswordUriAsync(AppUser user, string origin)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var route = "Login/ResetPassword";
            var completeUrl = new Uri(string.Concat(origin, "/", route));
            var resetUri = QueryHelpers.AddQueryString(completeUrl.ToString(), "token", token);
            return resetUri;
        }
    } //a
}
