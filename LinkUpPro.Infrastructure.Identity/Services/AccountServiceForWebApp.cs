using LinkUpPro.Core.Application.Dtos.Email;
using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Infrastructure.Identity.Entities;
using LinkUpPro.Core.Application.Dtos.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace LinkUpPro.Infrastructure.Identity.Services
{
    public class AccountServiceForWebApp : IAccountServiceForWebApp
    {
        // Latest valid link per user is stored (hashed) as an Identity user token: issuing a new one
        // replaces the previous link and using it removes it, so every link works only once.
        private const string TokenProvider = "LinkUpPro";
        private const string ActivationTokenName = "AccountActivation";
        private const string ResetTokenName = "PasswordReset";
        private static readonly TimeSpan ActivationLifetime = TimeSpan.FromHours(24);
        private static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(1);
        private static readonly TimeSpan ResendInterval = TimeSpan.FromMinutes(5);

        private const string InvalidCredentials = "El nombre de usuario o la contraseña son incorrectos.";
        private const string InactiveAccount = "Su cuenta se encuentra inactiva. Debe activarla mediante el enlace enviado a su correo electrónico.";
        private const string LockedAccount = "La cuenta se encuentra bloqueada temporalmente debido a varios intentos fallidos. Inténtelo nuevamente en 15 minutos o restablezca su contraseña.";
        private const string InvalidActivation = "El enlace de activación no es válido o ya fue utilizado.";
        private const string InvalidReset = "El enlace para restablecer la contraseña no es válido o ya fue utilizado.";

        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailService _emailService;
<<<<<<< HEAD
        private readonly IMemoryCache _cache;
        private readonly ILogger<AccountServiceForWebApp> _logger;

        public AccountServiceForWebApp(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager,
            IEmailService emailService, IMemoryCache cache, ILogger<AccountServiceForWebApp> logger)
=======
        
        public AccountServiceForWebApp(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IEmailService emailService)
>>>>>>> def86ca (Validacion de token resuelto)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _cache = cache;
            _logger = logger;
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
            };

            var user = await _userManager.FindByNameAsync(loginDto.UserName.Trim());

            // Unknown users get the same message as a wrong password; no failed-attempt counter applies.
            if (user == null)
            {
                response.HasError = true;
                response.Errors.Add(InvalidCredentials);
                return response;
            }

            // lockoutOnFailure: five consecutive wrong passwords lock the account for 15 minutes.
            var result = await _signInManager.PasswordSignInAsync(user, loginDto.Password, loginDto.RememberMe, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                response.HasError = true;

                if (result.IsLockedOut)
                {
                    response.Errors.Add(LockedAccount);
                }
                else if (result.IsNotAllowed && !user.EmailConfirmed)
                {
                    response.Errors.Add(InactiveAccount);
                }
                else
                {
                    response.Errors.Add(InvalidCredentials);
                }

                return response;
            }

            response.Id = user.Id;
            response.Email = user.Email ?? "";
            response.UserName = user.UserName ?? "";
            response.Name = user.FirstName;
            response.LastName = user.LastName;
            response.IsVerified = user.EmailConfirmed;

            return response;
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<bool> IsUserNameTakenAsync(string userName)
            => await _userManager.FindByNameAsync(userName.Trim()) != null;

        public async Task<bool> IsEmailTakenAsync(string email)
            => await _userManager.FindByEmailAsync(email.Trim()) != null;

        public async Task<RegisterResponseDto> RegisterUserAsync(RegisterUserDto saveDto, string origin)
        {
            RegisterResponseDto response = new()
            {
                Email = "",
                Id = "",
                LastName = "",
                Name = "",
                UserName = "",
                HasError = false,
                Errors = []
            };

            // Identity compares normalized (upper-case) values, so the checks ignore letter case.
            if (await IsUserNameTakenAsync(saveDto.UserName))
            {
                response.Errors.Add("Este nombre de usuario ya se encuentra registrado.");
            }

            if (await IsEmailTakenAsync(saveDto.Email))
            {
                response.Errors.Add("Este correo electrónico ya se encuentra registrado.");
            }

            if (response.Errors.Count > 0)
            {
                response.HasError = true;
                return response;
            }

            AppUser user = new()
            {
                Id = saveDto.Id,
                FirstName = saveDto.Name.Trim(),
                LastName = saveDto.LastName.Trim(),
                Email = saveDto.Email.Trim(),
                UserName = saveDto.UserName.Trim(),
                PhoneNumber = saveDto.Phone.Trim(),
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                ProfilePicturePath = saveDto.ProfileImage ?? ""
            };

            var result = await _userManager.CreateAsync(user, saveDto.Password);

            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Errors.AddRange(result.Errors.Select(e => TranslateIdentityError(e)));
                return response;
            }
<<<<<<< HEAD

            await SendActivationEmailAsync(user, origin);

            response.Id = user.Id;
            response.Email = user.Email ?? "";
            response.UserName = user.UserName ?? "";
            response.Name = user.FirstName;
            response.LastName = user.LastName;
            response.IsVerified = false;

            return response;
=======
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
>>>>>>> def86ca (Validacion de token resuelto)
        }

        public async Task<UserResponseDto> ConfirmAccountAsync(string userId, string token)
        {
            UserResponseDto response = new() { HasError = true, Errors = [] };

            var user = string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);

            if (user == null || user.EmailConfirmed || string.IsNullOrWhiteSpace(token))
            {
                response.Errors.Add(InvalidActivation);
                return response;
            }

            string? decoded = DecodeToken(token);
            var check = await CheckStoredTokenAsync(user, ActivationTokenName, decoded, ActivationLifetime);

            if (check == StoredTokenState.Expired)
            {
                response.Errors.Add("El enlace de activación ha vencido. Solicite uno nuevo.");
                return response;
            }

            if (check != StoredTokenState.Valid)
            {
                response.Errors.Add(InvalidActivation);
                return response;
            }

            var result = await _userManager.ConfirmEmailAsync(user, decoded!);

            if (!result.Succeeded)
            {
                response.Errors.Add(InvalidActivation);
                return response;
            }

            await _userManager.RemoveAuthenticationTokenAsync(user, TokenProvider, ActivationTokenName);

            response.HasError = false;
            response.Message = "Su cuenta fue activada correctamente. Ya puede iniciar sesión.";
            return response;
        }

        public async Task<UserResponseDto> ResendEmailActivationAsync(EmailConfirmationDto request, string origin)
        {
            UserResponseDto response = new()
            {
                HasError = false,
                Errors = [],
                Message = "Si la cuenta existe y todavía no ha sido activada, recibirá un nuevo enlace de activación."
            };

            string key = $"resend-activation:{request.UserName.Trim().ToUpperInvariant()}";

            // The wait applies to any entered name, so it does not reveal whether the account exists.
            if (_cache.TryGetValue(key, out _))
            {
                response.HasError = true;
                response.Errors.Add("Debe esperar cinco minutos antes de solicitar otro enlace de activación.");
                return response;
            }

            _cache.Set(key, true, ResendInterval);

            var user = await _userManager.FindByNameAsync(request.UserName.Trim());

            // Active accounts are left unchanged and receive no email.
            if (user != null && !user.EmailConfirmed)
            {
                await SendActivationEmailAsync(user, origin);
            }

            return response;
        }

        public async Task<UserResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            UserResponseDto response = new()
            {
                HasError = false,
                Errors = [],
                Message = "Si el nombre de usuario corresponde a una cuenta registrada, recibirá un enlace para restablecer su contraseña."
            };

            var user = await _userManager.FindByNameAsync(request.UserName.Trim());

            // Requesting a reset does not activate, deactivate or unlock the account.
            if (user != null)
            {
                string token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await StoreTokenAsync(user, ResetTokenName, token);

                string uri = BuildUri(request.Origin, "Login/ResetPassword", user.Id, token);

                await TrySendEmailAsync(user, "Restablecer contraseña - LinkUp Pro",
                    $"<p>Hola {WebUtility.HtmlEncode(user.FirstName)},</p>" +
                    "<p>Recibimos una solicitud para restablecer su contraseña. Este enlace vence en una hora:</p>" +
                    $"<p><a href='{uri}'>Restablecer contraseña</a></p>" +
                    "<p>Si usted no lo solicitó, ignore este correo.</p>");
            }

            return response;
        }

        public async Task<UserResponseDto> ValidateResetTokenAsync(string userId, string token)
        {
            UserResponseDto response = new() { HasError = true, Errors = [] };

            var user = string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
            var check = user == null
                ? StoredTokenState.Invalid
                : await CheckStoredTokenAsync(user, ResetTokenName, DecodeToken(token), ResetLifetime);

            if (check == StoredTokenState.Expired)
            {
                response.Errors.Add("El enlace para restablecer la contraseña ha vencido. Solicite uno nuevo.");
            }
            else if (check != StoredTokenState.Valid)
            {
                response.Errors.Add(InvalidReset);
            }
            else
            {
                response.HasError = false;
            }

            return response;
        }

        public async Task<UserResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var response = await ValidateResetTokenAsync(request.Id, request.Token);

            if (response.HasError)
            {
                return response;
            }

            var user = (await _userManager.FindByIdAsync(request.Id))!;

            // ResetPasswordAsync also renews the security stamp, which invalidates every previous session.
            var result = await _userManager.ResetPasswordAsync(user, DecodeToken(request.Token)!, request.Password);

            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Errors.AddRange(result.Errors.Select(e => TranslateIdentityError(e)));
                return response;
            }

            await _userManager.RemoveAuthenticationTokenAsync(user, TokenProvider, ResetTokenName);
            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);

            // The activation state is intentionally not modified.
            response.Message = "Su contraseña fue restablecida correctamente. Ya puede iniciar sesión.";
            return response;
        }

        public async Task<EditResponseDto> EditUserAsync(EditUserDto dto)
        {
            EditResponseDto response = new() { HasError = false, Errors = [] };

            var user = await _userManager.FindByIdAsync(dto.Id);

            if (user == null)
            {
                response.HasError = true;
                response.Errors.Add("No posee permisos para modificar este perfil.");
                return response;
            }

            bool wantsPasswordChange = !string.IsNullOrEmpty(dto.CurrentPassword) || !string.IsNullOrEmpty(dto.NewPassword);

            if (wantsPasswordChange)
            {
                if (string.IsNullOrEmpty(dto.CurrentPassword) || string.IsNullOrEmpty(dto.NewPassword))
                {
                    response.HasError = true;
                    response.Errors.Add("Para cambiar su contraseña debe completar la contraseña actual, la nueva contraseña y su confirmación.");
                    return response;
                }

                if (!await _userManager.CheckPasswordAsync(user, dto.CurrentPassword))
                {
                    response.HasError = true;
                    response.Errors.Add("La contraseña actual es incorrecta.");
                    return response;
                }

                if (dto.CurrentPassword == dto.NewPassword)
                {
                    response.HasError = true;
                    response.Errors.Add("La nueva contraseña debe ser diferente de la contraseña actual.");
                    return response;
                }

                foreach (var validator in _userManager.PasswordValidators)
                {
                    var validation = await validator.ValidateAsync(_userManager, user, dto.NewPassword);

                    if (!validation.Succeeded)
                    {
                        response.HasError = true;
                        response.Errors.AddRange(validation.Errors.Select(e => TranslateIdentityError(e)));
                    }
                }

                if (response.HasError)
                {
                    return response;
                }
            }

            response.PreviousProfileImage = user.ProfilePicturePath;

            user.FirstName = dto.Name.Trim();
            user.LastName = dto.LastName.Trim();
            user.PhoneNumber = dto.Phone.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.ProfileImage))
            {
                user.ProfilePicturePath = dto.ProfileImage;
            }

            if (wantsPasswordChange)
            {
                // Personal data, hash and a new security stamp are saved in one update: nothing partial,
                // and the new stamp invalidates every session, including "Mantener sesión iniciada" cookies.
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, dto.NewPassword!);
                user.SecurityStamp = Guid.NewGuid().ToString("N").ToUpperInvariant();
                response.PasswordChanged = true;
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                response.HasError = true;
                response.PasswordChanged = false;
                response.Errors.AddRange(result.Errors.Select(e => TranslateIdentityError(e)));
            }

            return response;
        }

        public async Task<List<UserDto>> GetAllUserAsync(bool? isActive = true)
        {
            var users = _userManager.Users.AsNoTracking();

            if (isActive == true)
            {
                users = users.Where(u => u.EmailConfirmed);
            }

            return (await users.ToListAsync()).Select(ToDto).ToList();
        }
<<<<<<< HEAD

        public async Task<UserDto?> GetUserByUserNameAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            return user == null ? null : ToDto(user);
        }

        public async Task<UserDto?> GetUserByIdAsync(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            return user == null ? null : ToDto(user);
        }

        #region Private helpers

        private enum StoredTokenState { Valid, Invalid, Expired }

        private static UserDto ToDto(AppUser user) => new()
        {
            Id = user.Id,
            Name = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
            Phone = user.PhoneNumber,
            ProfileImage = user.ProfilePicturePath,
            isVerified = user.EmailConfirmed
        };

        private async Task SendActivationEmailAsync(AppUser user, string origin)
        {
            string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await StoreTokenAsync(user, ActivationTokenName, token);

            string uri = BuildUri(origin, "Login/ConfirmEmail", user.Id, token);

            await TrySendEmailAsync(user, "Active su cuenta - LinkUp Pro",
                $"<p>Hola {WebUtility.HtmlEncode(user.FirstName)},</p>" +
                "<p>Gracias por registrarse en LinkUp Pro. Active su cuenta con el siguiente enlace (vence en 24 horas):</p>" +
                $"<p><a href='{uri}'>Activar cuenta</a></p>");
        }

        // A failed email does not undo the account; the user can request a new link.
        private async Task TrySendEmailAsync(AppUser user, string subject, string body)
        {
            try
            {
                await _emailService.SendEmailAsync(new EmailRequestDto { To = user.Email, Subject = subject, HtmlBody = body });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The account email could not be sent.");
            }
        }

        private async Task StoreTokenAsync(AppUser user, string name, string token)
            => await _userManager.SetAuthenticationTokenAsync(user, TokenProvider, name,
                $"{Hash(token)}|{DateTime.UtcNow.Ticks}");

        private async Task<StoredTokenState> CheckStoredTokenAsync(AppUser user, string name, string? token, TimeSpan lifetime)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return StoredTokenState.Invalid;
            }

            string? stored = await _userManager.GetAuthenticationTokenAsync(user, TokenProvider, name);
            string[] parts = stored?.Split('|') ?? [];

            if (parts.Length != 2 || !long.TryParse(parts[1], out long ticks) ||
                !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(parts[0]), Encoding.UTF8.GetBytes(Hash(token))))
            {
                return StoredTokenState.Invalid;
            }

            return DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) > lifetime
                ? StoredTokenState.Expired
                : StoredTokenState.Valid;
        }

        private static string Hash(string value)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        private static string? DecodeToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static string BuildUri(string origin, string route, string userId, string token)
        {
            string encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            string uri = QueryHelpers.AddQueryString($"{origin.TrimEnd('/')}/{route}", "userId", userId);
            return QueryHelpers.AddQueryString(uri, "token", encoded);
        }

        private static string TranslateIdentityError(IdentityError error) => error.Code switch
        {
            "PasswordTooShort" => "La contraseña debe tener al menos ocho caracteres.",
            "PasswordRequiresUpper" => "La contraseña debe contener al menos una letra mayúscula.",
            "PasswordRequiresLower" => "La contraseña debe contener al menos una letra minúscula.",
            "PasswordRequiresDigit" => "La contraseña debe contener al menos un número.",
            "PasswordRequiresNonAlphanumeric" => "La contraseña debe contener al menos un carácter especial.",
            "DuplicateUserName" => "Este nombre de usuario ya se encuentra registrado.",
            "DuplicateEmail" => "Este correo electrónico ya se encuentra registrado.",
            "InvalidEmail" => "Debe ingresar un correo electrónico válido.",
            "InvalidUserName" => "El nombre de usuario solo puede contener letras, números y los caracteres - . _ @ +",
            "InvalidToken" => InvalidReset,
            _ => "Ocurrió un error al procesar la solicitud. Inténtelo nuevamente."
        };

        #endregion
=======
>>>>>>> def86ca (Validacion de token resuelto)
    }
}
