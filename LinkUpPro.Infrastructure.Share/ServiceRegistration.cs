using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Infrastructure.Share.Services;
using LinkUpPro.Core.Domain.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace LinkUpPro.Infrastructure.Share{
    public static class ServiceRegistration
    {
        public static void AddSharedLayerIoc(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<EmailSettings>(configuration.GetSection("MailSettings"));
            services.AddScoped<IEmailService, EmailService>();
        }
    }
}