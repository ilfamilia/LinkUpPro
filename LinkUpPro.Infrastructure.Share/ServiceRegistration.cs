using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Infrastructure.Share.Services;
using LinkUpPro.Core.Domain.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace LinkUpPro.Infrastructure.Share{
    public static class ServiceRegistration
    {
        // webRootPath: physical folder served as static files, where uploaded images are stored.
        public static void AddSharedLayerIoc(this IServiceCollection services, IConfiguration configuration,
            string webRootPath)
        {
            services.Configure<EmailSettings>(configuration.GetSection("MailSettings"));
            services.Configure<ImageStorageSettings>(opt => opt.RootPath = webRootPath);
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IImageStorageService, ImageStorageService>();
        }
    }
}
