using LinkUpPro.Core.Application.Dtos.Email;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Settings;
using Microsoft.Extensions.Options;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace LinkUpPro.Infrastructure.Share.Services
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
                    Sender = MailboxAddress.Parse(_emailSttings.EmailFrom),
                    From = { new MailboxAddress(_emailSttings.DisplayName, _emailSttings.EmailFrom) },
                    Subject = emailRequestDto.Subject,
                };

                foreach (var to in emailRequestDto.ToRange ?? [])
                {
                    email.To.Add(MailboxAddress.Parse(to));
                }

                BodyBuilder builder = new()
                {
                    HtmlBody = emailRequestDto.HtmlBody
                };

                email.Body = builder.ToMessageBody();

                if (!string.IsNullOrWhiteSpace(_emailSttings.PickupDirectory))
                {
                    // Local development: store the message so its links can be opened without an SMTP server.
                    Directory.CreateDirectory(_emailSttings.PickupDirectory);
                    string path = Path.Combine(_emailSttings.PickupDirectory, $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.eml");
                    await email.WriteToAsync(path);
                    return;
                }

                using MailKit.Net.Smtp.SmtpClient smtp = new();
                await smtp.ConnectAsync(_emailSttings.SmtpHost, _emailSttings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailSttings.SmtpUser, _emailSttings.SmtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email.");
                throw;
            }
        }
    }
}
