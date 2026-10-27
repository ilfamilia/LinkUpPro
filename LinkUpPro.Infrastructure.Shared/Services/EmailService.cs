using LinkUpPro.Core.Application.Dtos.Email;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Settings;
using Microsoft.Extensions.Options;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace LinkUpPro.Infrastructure.Shared.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSttings;
        private readonly ILogger<EmailService> _logger;
        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSttings = emailSettings.Value;
            _logger = logger;
        }
        public async Task SendEmailAsync(EmailRequestDto emailRequestDto)
        {
            try
            {
                emailRequestDto.ToRange?.Add(emailRequestDto.To ?? "");
                MimeMessage email = new()
                {
                    Subject = emailRequestDto.Subject
                };

                email.From.Add(new MailboxAddress(_emailSttings.DisplayName, _emailSttings.EmailFrom));

                foreach (var to in emailRequestDto.ToRange ?? [])
                {
                    email.To.Add(MailboxAddress.Parse(to));
                }

                BodyBuilder builder = new()
                {
                    HtmlBody = emailRequestDto.HtmlBody
                };

                email.Body = builder.ToMessageBody();

                using MailKit.Net.Smtp.SmtpClient smtp = new();
                await smtp.ConnectAsync(_emailSttings.SmtpHost, _emailSttings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailSttings.SmtpUser, _emailSttings.SmtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error al enviar el correo electrónico.");

                throw;
            }
        }
    }
}
