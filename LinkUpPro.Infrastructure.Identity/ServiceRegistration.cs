using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Infrastructure.Identity.Contexts;
using LinkUpPro.Infrastructure.Identity.Entities;
using LinkUpPro.Infrastructure.Identity.Services;
using LinkUpPro.Infrastructure.Identity.TokenProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkUpPro.Infrastructure.Identity
{
    public static class ServiceRegistration
    {
        public static void AddIdentityLayerIocForWebApp(this IServiceCollection services, IConfiguration config)
        {
            GeneralConfiguration(services, config);
            // Publish account summaries without exposing Identity entities to Application.
            services.AddScoped<LinkUpPro.Core.Application.Interfaces.IUserDirectoryService, LinkUpPro.Infrastructure.Identity.Services.UserDirectoryService>();

            #region Identity
            services.Configure<IdentityOptions>(opt => 
            {
                // Password
                opt.Password.RequiredLength = 8;
                opt.Password.RequireDigit = true;
                opt.Password.RequireNonAlphanumeric = true;
                opt.Password.RequireLowercase = true;
                opt.Password.RequireUppercase = true;

                // Lockout
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                opt.Lockout.MaxFailedAccessAttempts = 5;
                opt.Lockout.AllowedForNewUsers = true;

                // User
                opt.User.RequireUniqueEmail = true;

                // Sign In
                opt.SignIn.RequireConfirmedEmail = true;

                // Token Providers
                opt.Tokens.EmailConfirmationTokenProvider = "EmailConfirmation";
                opt.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
            });

            services.AddIdentityCore<AppUser>()
                .AddSignInManager()
                .AddEntityFrameworkStores<IdentityContext>()
                .AddTokenProvider<DataProtectorTokenProvider<AppUser>>(TokenOptions.DefaultProvider)
                .AddTokenProvider<EmailConfirmationTokenProvider>("EmailConfirmation");

            // Password reset: 1 hour
            services.Configure<DataProtectionTokenProviderOptions>(opt => 
            { 
                opt.TokenLifespan = TimeSpan.FromHours(1);
            });

            // Email confirmation: 24 hours
            services.Configure<EmailConfirmationTokenProviderOptions>(opt =>
            {
                opt.Name = "LinkUpPro.EmailConfirmation";
                opt.TokenLifespan = TimeSpan.FromHours(24);
            });

            // Security Stamp Configuration
            services.Configure<SecurityStampValidatorOptions>(opt =>
            {
                opt.ValidationInterval = TimeSpan.Zero;
            });

            services.AddAuthentication(opt =>
            {
                opt.DefaultScheme = IdentityConstants.ApplicationScheme;
                opt.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                opt.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
            }).AddCookie(IdentityConstants.ApplicationScheme, opt => 
            {
                opt.LoginPath = "/Login";
                opt.AccessDeniedPath = "/Login/AccessDenied";

                // Normal session
                opt.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                opt.SlidingExpiration = true;

                // Cookie security
                opt.Cookie.HttpOnly = true;
                opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                opt.Cookie.SameSite = SameSiteMode.Lax;

                opt.Events.OnSigningIn = context =>
                {
                    if (context.Properties.IsPersistent)
                    {
                        // Remember Me: maximum 7 days
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7);

                        context.Properties.AllowRefresh = false;
                    }

                    return Task.CompletedTask;
                };

                // Tell the login page why the user arrived: an existing session that expired (inactivity)
                // or an anonymous visit to a protected page.
                opt.Events.OnRedirectToLogin = context =>
                {
                    string cookieName = context.Options.Cookie.Name ?? ".AspNetCore.Identity.Application";
                    string reason = context.Request.Cookies.ContainsKey(cookieName) ? "timeout" : "required";
                    context.Response.Redirect(Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(context.RedirectUri, "reason", reason));
                    return Task.CompletedTask;
                };

                opt.Events.OnValidatePrincipal = async context =>
                {
                    await SecurityStampValidator.ValidatePrincipalAsync(context);

                    // Prevent extending persistent sessions during stamp validation.
                    if (context.Properties.IsPersistent)
                    {
                        context.ShouldRenew = false;
                    }
                };
            });

            services.AddAuthorization();
            services.AddMemoryCache();
            services.AddScoped<IAccountServiceForWebApp, AccountServiceForWebApp>();
            #endregion

            #region Services IOC
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, CurrentUserContext>();
            services.AddScoped<IUserAccountStatusService, UserAccountStatusService>();
            #endregion
        }

        #region Private Methods
        private static void GeneralConfiguration(IServiceCollection services, IConfiguration config)
        {
            #region Contexts
            if (config.GetValue<bool>("UseInMemoryDatabase"))
            {
                services.AddDbContext<IdentityContext>(opt => opt.UseInMemoryDatabase("IdentityDb"));
            }
            else
            {
                var connectionString = config.GetConnectionString("DefaultConnection");

                services.AddDbContext<IdentityContext>(

                    (serviceProvider, opt) =>
                    {
                        opt.UseSqlServer(connectionString, sql =>
                        {
                            sql.MigrationsAssembly(typeof(IdentityContext).Assembly.FullName);
                            sql.MigrationsHistoryTable("__EFMigrationsHistoryIdentity", "dbo");
                        });
                    },
                    contextLifetime: ServiceLifetime.Scoped,
                    optionsLifetime: ServiceLifetime.Scoped  
                );
            }
            #endregion
        }
        #endregion
    }
}
