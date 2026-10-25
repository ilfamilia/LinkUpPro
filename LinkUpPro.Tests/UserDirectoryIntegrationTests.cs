using LinkUpPro.Infrastructure.Identity.Contexts;
using LinkUpPro.Infrastructure.Identity.Entities;
using LinkUpPro.Infrastructure.Identity.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LinkUpPro.Tests;

/// <summary>Checks the real Identity adapter and keeps activation separate from temporary login lockouts.</summary>
[Collection("SqlDatabase")]
public sealed class UserDirectoryIntegrationTests(SqlDatabaseFixture database)
{
    [Fact]
    public async Task DirectoryUsesConfirmedEmailAndDoesNotExposePrivateFields()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>().UseSqlServer(database.ConnectionString).Options;
        await using var context = new IdentityContext(options);
        var prefix = Guid.NewGuid().ToString("N");
        var active = new AppUser { Id = prefix, UserName = prefix + "Active", NormalizedUserName = (prefix + "Active").ToUpperInvariant(),
            FirstName = "Public", LastName = "Profile", ProfilePicturePath = "/images/profile.png", CreatedAt = DateTime.UtcNow,
            Email = "private@example.test", EmailConfirmed = true, LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15) };
        var inactive = new AppUser { Id = Guid.NewGuid().ToString("N"), UserName = prefix + "Inactive",
            NormalizedUserName = (prefix + "Inactive").ToUpperInvariant(), FirstName = "Inactive", LastName = "Profile",
            ProfilePicturePath = "/images/profile.png", CreatedAt = DateTime.UtcNow, EmailConfirmed = false };
        context.Users.AddRange(active, inactive);
        await context.SaveChangesAsync();
        var directory = new UserDirectoryService(context);
        Assert.True((await directory.GetByIdAsync(active.Id))!.IsActive);
        Assert.False((await directory.GetByIdAsync(inactive.Id))!.IsActive);
        Assert.Equal(active.Id, Assert.Single(await directory.SearchActiveAsync(" " + prefix + "aCTive ")).Id);
        Assert.Equal(2, (await directory.GetByIdsAsync([active.Id, inactive.Id])).Count);
        Assert.DoesNotContain(typeof(LinkUpPro.Core.Application.Dtos.Social.UserSummaryDto).GetProperties(),
            property => property.Name is "Email" or "PhoneNumber" or "Role" or "PasswordHash");
    }
}
