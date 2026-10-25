using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkUpPro.Infrastructure.Identity.TokenProviders
{
    public class EmailConfirmationTokenProvider
        : DataProtectorTokenProvider<AppUser>
    {
        public EmailConfirmationTokenProvider(IDataProtectionProvider dataProtectionProvider,
            IOptions<EmailConfirmationTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<AppUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }
}