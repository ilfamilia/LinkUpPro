using System.Security.Claims;
using System.Text.Encodings.Web;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkUpPro.Tests;

/// <summary>Hosts the actual MVC application with isolated SQL and authentication confined to the test assembly.</summary>
internal sealed class ModuleWebFactory(SqlDatabaseFixture database, TestUserDirectory users, TestClock clock) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "LinkUpPro.slnx"))) root = root.Parent;
        builder.UseContentRoot(Path.Combine(root!.FullName, "LinkUpPro"));
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<LinkUpProAppContext>();
            services.RemoveAll<DbContextOptions<LinkUpProAppContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<LinkUpProAppContext>>();
            services.AddDbContext<LinkUpProAppContext>(options => options.UseSqlServer(database.ConnectionString));
            services.RemoveAll<IUserDirectoryService>();
            services.AddSingleton<IUserDirectoryService>(users);
            // The publication module checks activation through Identity; answer from the same test directory.
            services.RemoveAll<IUserAccountStatusService>();
            services.AddSingleton<IUserAccountStatusService>(new TestAccountStatus(users));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(clock);
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
        });
    }
}

/// <summary>Authenticates explicit test headers; no equivalent handler or bypass exists in production.</summary>
internal sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "ModuleIntegrationTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var id = Request.Headers["X-Test-User"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

/// <summary>Reports account activation from the isolated test directory instead of Identity tables.</summary>
internal sealed class TestAccountStatus(TestUserDirectory users) : IUserAccountStatusService
{
    public Task<bool> IsUserActiveAsync(string userId)
        => Task.FromResult(users.Users.Any(u => u.Id == userId && u.IsActive));

    public Task<HashSet<string>> GetActiveUserIdsAsync(IEnumerable<string> userIds)
        => Task.FromResult(users.Users.Where(u => u.IsActive && userIds.Contains(u.Id)).Select(u => u.Id).ToHashSet());
}
