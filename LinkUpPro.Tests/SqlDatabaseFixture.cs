using LinkUpPro.Infrastructure.Persistence.Contexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LinkUpPro.Tests;

/// <summary>Creates a uniquely named SQL test database and never touches the application's configured database.</summary>
public sealed class SqlDatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; }
    public string DatabaseName { get; } = "LinkUpProTests_" + Guid.NewGuid().ToString("N");

    public SqlDatabaseFixture()
    {
        var source = Environment.GetEnvironmentVariable("LINKUPPRO_TEST_CONNECTION")
            ?? "Server=.;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;Connection Timeout=5";
        var builder = new SqlConnectionStringBuilder(source) { InitialCatalog = DatabaseName };
        ConnectionString = builder.ConnectionString;
    }

    /// <summary>Builds a fresh context sharing only this fixture's isolated database.</summary>
    public LinkUpProAppContext CreateContext() => new(new DbContextOptionsBuilder<LinkUpProAppContext>()
        .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryPersistence", "dbo")).Options);

    /// <summary>Applies both migration histories to an isolated database before integration checks.</summary>
    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        var identityOptions = new DbContextOptionsBuilder<LinkUpPro.Infrastructure.Identity.Contexts.IdentityContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistoryIdentity", "dbo")).Options;
        await using var identity = new LinkUpPro.Infrastructure.Identity.Contexts.IdentityContext(identityOptions);
        await identity.Database.MigrateAsync();
    }

    /// <summary>Deletes only the random test database after verifying its generated name.</summary>
    public async Task DisposeAsync()
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(DatabaseName, "^LinkUpProTests_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Refusing to delete a database not owned by this fixture.");
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}

/// <summary>Shares the isolated database while preventing unrelated test suites from interfering with SQL state.</summary>
[CollectionDefinition("SqlDatabase", DisableParallelization = true)]
public sealed class SqlDatabaseCollection : ICollectionFixture<SqlDatabaseFixture>;
