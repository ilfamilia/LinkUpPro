using LinkUpPro.Core.Application.Dtos.Email;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailRequestDto emailRequestDto);
    }
}