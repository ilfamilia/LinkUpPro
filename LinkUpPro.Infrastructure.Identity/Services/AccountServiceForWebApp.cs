using LinkUpPro.Core.Application.Dtos.Email;
using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace LinkUpPro.Infrastructure.Identity.Services
{
    public class AccountServiceForWebApp : IAccountServiceForWebApp
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountServiceForWebApp> _logger;

        public AccountServiceForWebApp(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
            IEmailService emailService, ILogger<AccountServiceForWebApp> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<LoginResponseDto> AuthenticateAsync(LoginDto loginDto)
        {
            LoginResponseDto response = new()
            {
                Id = string.Empty,
                Name = string.Empty,
                LastName = string.Empty,
                Email = string.Empty,
                UserName = string.Empty,
                HasError = false,
                Errors = []
            };

            const string invalidCredentialsMessage = "El nombre de usuario o la contraseña son incorrectos.";

            const string lockedAccountMessage = "La cuenta se encuentra bloqueada temporalmente debido " +
                "a varios intentos fallidos. Inténtelo nuevamente en " +
                "15 minutos o restablezca su contraseña.";

            if (string.IsNullOrWhiteSpace(loginDto.UserName) ||
                string.IsNullOrEmpty(loginDto.Password))
            {
                response.HasError = true;
                response.Errors.Add(invalidCredentialsMessage);
                return response;
            }

            AppUser? user = await _userManager.FindByNameAsync(loginDto.UserName.Trim());

            if (user == null)
            {
                response.HasError = true;
                response.Errors.Add(invalidCredentialsMessage);
                return response;
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                response.HasError = true;
                response.Errors.Add(lockedAccountMessage);
                return response;
            }

            if (!user.EmailConfirmed)
            {
                response.HasError = true;
                response.Errors.Add("Su cuenta se encuentra inactiva. Debe activarla " +
                    "mediante el enlace enviado a su correo electrónico.");

                return response;
            }

            SignInResult result = await _signInManager.PasswordSignInAsync(user, loginDto.Password,
                loginDto.RememberMe, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                response.HasError = true;
                response.Errors.Add(lockedAccountMessage);
                return response;
            }

            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Errors.Add(invalidCredentialsMessage);
                return response;
            }

            response.Id = user.Id;
            response.Name = user.FirstName;
            response.LastName = user.LastName;
            response.Email = user.Email ?? string.Empty;
            response.UserName = user.UserName ?? string.Empty;
            response.IsVerified = user.EmailConfirmed;

            return response;
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<RegisterResponseDto> RegisterUserAsync(RegisterUserDto saveDto, string origin)
        {
            RegisterResponseDto RegisterRes = new()
            {
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
                RegisterRes.Errors.Add("Este correo electrónico ya se encuentra registrado.");
                return RegisterRes;
            }

            var userWithSameUserName = await _userManager.FindByNameAsync(saveDto.UserName);
            if (userWithSameUserName != null)
            {
                RegisterRes.HasError = true;
                RegisterRes.Errors.Add("Este nombre de usuario ya se encuentra registrado.");
                return RegisterRes;
            }

            AppUser user = new()
            {
                Id = saveDto.Id,
                FirstName = saveDto.Name,
                LastName = saveDto.LastName,
                Email = saveDto.Email,
                UserName = saveDto.UserName,
                PhoneNumber = saveDto.Phone,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                ProfilePicturePath = saveDto.ProfileImage ?? string.Empty
            };

            var result = await _userManager.CreateAsync(user, saveDto.Password);

            if (result.Succeeded)
            {
                RegisterRes.Id = user.Id;
                RegisterRes.Email = user.Email ?? string.Empty;
                RegisterRes.UserName = user.UserName ?? string.Empty;
                RegisterRes.Name = user.FirstName;
                RegisterRes.LastName = user.LastName;
                RegisterRes.IsVerified = user.EmailConfirmed;
                RegisterRes.HasError = false;


                try
                {
                    string verificationUri = await GetVerificationEmailUriAsync(user, origin);

                    await _emailService.SendEmailAsync(new EmailRequestDto
                    {
                        To = user.Email!,
                        Subject = "Activación de cuenta - LinkUp Pro",
                        HtmlBody = "<p>Para activar su cuenta, haga clic en el siguiente enlace:</p>" +
                            $"<a href=\"{System.Net.WebUtility.HtmlEncode(verificationUri)}\">Activar cuenta</a>"
                    });

                    RegisterRes.ActivationEmailSent = true;

                    try
                    {
                        user.LastActivationEmailSentAtUtc = DateTime.UtcNow;

                        IdentityResult updateResult = await _userManager.UpdateAsync(user);

                        if (!updateResult.Succeeded)
                        {
                            _logger.LogWarning("No se pudo registrar la fecha del correo de activación del usuario {UserId}.",
                                user.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al guardar la fecha de activación del usuario {UserId}.",
                            user.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudo enviar el correo de activación del usuario {UserId}.",
                        user.Id);

                    RegisterRes.ActivationEmailSent = false;
                }

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
            const string invalidTokenMessage = "El enlace de activación no es válido, ha vencido o ya fue utilizado";

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                return invalidTokenMessage;
            }

            AppUser? user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.EmailConfirmed)
            {
                return invalidTokenMessage;
            }

            string decodedToken;

            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                return invalidTokenMessage;
            }

            IdentityResult result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                return invalidTokenMessage;
            }

            return "Su cuenta fue activada correctamente. Ya puede iniciar sesión.";
        }

        public async Task<string> ResendEmailActivationAsync(EmailConfirmationDto request, string origin)
        {
            const string message = "Si la cuenta existe y todavía no ha sido activada, " +
                "recibirá un nuevo enlace de activación.";

            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                return message;
            }

            AppUser? user = await _userManager.FindByNameAsync(request.UserName.Trim());

            if (user == null || user.EmailConfirmed)
            {
                return message;
            }

            DateTime now = DateTime.UtcNow;

            if (user.LastActivationEmailSentAtUtc.HasValue)
            {
                TimeSpan elapsed = now - user.LastActivationEmailSentAtUtc.Value;

                if (elapsed < TimeSpan.FromMinutes(5))
                {
                    return message;
                }
            }

            try
            {
                user.LastActivationEmailSentAtUtc = now;

                IdentityResult updateResult = await _userManager.UpdateSecurityStampAsync(user);

                if (!updateResult.Succeeded)
                {
                    _logger.LogWarning("No se pudo actualizar el token de activación para el usuario {UserId}.",
                        user.Id);

                    return message;
                }

                string verificationUri = await GetVerificationEmailUriAsync(user, origin);

                await _emailService.SendEmailAsync(new EmailRequestDto
                {
                    To = user.Email!,
                    Subject = "Activación de cuenta - LinkUp Pro",
                    HtmlBody = "<p>Para activar su cuenta, utilice el siguiente enlace:</p>" +
                        $"<a href=\"{verificationUri}\">Activar cuenta</a>"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo procesar el reenvío de activación del usuario {UserId}.",
                    user.Id);
            }

            return message;
        }

        public async Task<List<UserDto>> GetAllUserAsync(bool? isActive = true)
        {
            List<UserDto> ListUsersDto = [];

            var users = _userManager.Users;

            if (isActive != null && isActive == true)
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
            if (user == null)
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
            if (user == null)
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


        public async Task<EditResponseDto> EditUserAsync(EditUserDto editUserDto, string origin, bool? isCreated = false)
        {
            EditResponseDto response = new()
            {
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

            AppUser? user = await _userManager.FindByIdAsync(editUserDto.Id);

            if (user == null)
            {
                response.HasError = true;
                response.Errors.Add("El usuario no existe.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(editUserDto.Name) ||
                string.IsNullOrWhiteSpace(editUserDto.LastName) ||
                string.IsNullOrWhiteSpace(editUserDto.Phone))
            {
                response.HasError = true;
                response.Errors.Add("El nombre, apellido y teléfono son obligatorios.");

                return response;
            }

            bool wantsPasswordChange = !string.IsNullOrWhiteSpace(editUserDto.CurrentPassword) ||
                !string.IsNullOrWhiteSpace(editUserDto.NewPassword) ||
                !string.IsNullOrWhiteSpace(editUserDto.ConfirmPassword);

            if (wantsPasswordChange)
            {
                if (string.IsNullOrWhiteSpace(editUserDto.CurrentPassword) ||
                    string.IsNullOrWhiteSpace(editUserDto.NewPassword) ||
                    string.IsNullOrWhiteSpace(editUserDto.ConfirmPassword))
                {
                    response.HasError = true;
                    response.Errors.Add("Para cambiar su contraseña debe completar la contraseña " +
                        "actual, la nueva contraseña y su confirmación.");

                    return response;
                }

                if (editUserDto.NewPassword != editUserDto.ConfirmPassword)
                {
                    response.HasError = true;
                    response.Errors.Add("La nueva contraseña y su confirmación no coinciden.");

                    return response;
                }

                if (await _userManager.CheckPasswordAsync(user, editUserDto.NewPassword))
                {
                    response.HasError = true;
                    response.Errors.Add("La nueva contraseña debe ser diferente de la contraseña actual.");

                    return response;
                }
            }

            user.FirstName = editUserDto.Name.Trim();
            user.LastName = editUserDto.LastName.Trim();
            user.PhoneNumber = editUserDto.Phone.Trim();

            if (!string.IsNullOrWhiteSpace(editUserDto.ProfileImage))
            {
                user.ProfilePicturePath = editUserDto.ProfileImage;
            }

            user.UpdatedAt = DateTime.UtcNow;

            IdentityResult result;

            if (wantsPasswordChange)
            {
                result = await _userManager.ChangePasswordAsync(
                    user,
                    editUserDto.CurrentPassword!,
                    editUserDto.NewPassword!);
            }
            else
            {
                result = await _userManager.UpdateAsync(user);
            }

            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Errors.AddRange(
                    result.Errors.Select(e => e.Description));

                return response;
            }

            return new EditResponseDto
            {
                Id = user.Id,
                Name = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Phone = user.PhoneNumber ?? "",
                ProfileImage = user.ProfilePicturePath,
                CurrentPassword = string.Empty,
                NewPassword = string.Empty,
                HasError = false,
                Errors = []
            };
        }

        public async Task<UserResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            UserResponseDto response = new()
            {
                HasError = false,
                Errors = []
            };

            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                return response;
            }

            AppUser? user = await _userManager.FindByNameAsync(request.UserName.Trim());

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return response;
            }

            try
            {
                string resetPasswordUri = await GetResetPasswordUriAsync(user, request.Origin);

                await _emailService.SendEmailAsync(new EmailRequestDto
                {
                    To = user.Email,
                    Subject = "Restablecer contraseña - LinkUp Pro",
                    HtmlBody = "<p>Hemos recibido una solicitud para restablecer su contraseña.</p>" +
                        "<p>Utilice el siguiente enlace:</p>" +
                        $"<a href=\"{System.Net.WebUtility.HtmlEncode(resetPasswordUri)}\">" +
                        "Restablecer contraseña</a>" +
                        "<p>Si no realizó esta solicitud, ignore este correo.</p>"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo procesar la recuperación de contraseña del usuario {UserId}.",
                    user.Id);
            }

            return response;
        }

        public async Task<UserResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            UserResponseDto response = new()
            {
                HasError = false,
                Errors = []
            };

            const string invalidTokenMessage = "El enlace para restablecer la contraseña " +
                "no es válido, ha vencido o ya fue utilizado.";

            if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Token))
            {
                response.HasError = true;
                response.Errors.Add(invalidTokenMessage);
                return response;
            }

            AppUser? user = await _userManager.FindByIdAsync(request.Id);

            if (user == null)
            {
                response.HasError = true;
                response.Errors.Add(invalidTokenMessage);
                return response;
            }

            byte[] receivedHash = SHA256.HashData(Encoding.UTF8.GetBytes(request.Token));

            if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash) ||
                !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(user.PasswordResetTokenHash),
                    receivedHash))
            {
                response.HasError = true;
                response.Errors.Add(invalidTokenMessage);

                return response;
            }

            string token;

            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                response.HasError = true;
                response.Errors.Add(invalidTokenMessage);
                return response;
            }

            IdentityResult result = await _userManager.ResetPasswordAsync(user, token,
                request.Password);

            if (!result.Succeeded)
            {
                response.HasError = true;

                if (result.Errors.Any(e => e.Code == "InvalidToken"))
                {
                    response.Errors.Add(invalidTokenMessage);
                }
                else
                {
                    response.Errors.Add("No se pudo restablecer la contraseña. " +
                        "Verifique que cumpla los requisitos de seguridad");
                }

                return response;
            }

            user.PasswordResetTokenHash = null;

            IdentityResult clearResult = await _userManager.UpdateAsync(user);

            if (!clearResult.Succeeded)
            {
                _logger.LogWarning("No se pudo limpiar el token de recuperación del usuario {UserId}.",
                    user.Id);
            }

            IdentityResult resetAttempts = await _userManager.ResetAccessFailedCountAsync(user);

            IdentityResult unlockResult = await _userManager.SetLockoutEndDateAsync(user, null);

            if (!resetAttempts.Succeeded || !unlockResult.Succeeded)
            {
                _logger.LogError("La contraseña del usuario {UserId} fue restablecida, " +
                    "pero no se pudo completar el desbloqueo.", user.Id);

                response.HasError = true;
                response.Errors.Add("La contraseña fue cambiada, pero no se pudo " +
                    "completar el desbloqueo de la cuenta.");

                return response;
            }

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
            string token = await _userManager.GeneratePasswordResetTokenAsync(user);

            string encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Guardar únicamente la huella del token.
            byte[] tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(encodedToken));

            user.PasswordResetTokenHash = Convert.ToHexString(tokenHash);

            IdentityResult result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException("No se pudo registrar la solicitud de recuperación.");
            }

            string url = $"{origin.TrimEnd('/')}/Login/ResetPassword";

            url = QueryHelpers.AddQueryString(url, "userId", user.Id);

            return QueryHelpers.AddQueryString(url, "token", encodedToken);
        }
    }
}