using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Infrastructure.Identity.Contexts
{
    public class IdentityContext : IdentityUserContext<AppUser>
    {
        public IdentityContext(DbContextOptions<IdentityContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.Property(x => x.LoginProvider).HasMaxLength(128);
                entity.Property(x => x.ProviderKey).HasMaxLength(128);
            });

            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.Property(x => x.LoginProvider).HasMaxLength(128);
                entity.Property(x => x.Name).HasMaxLength(128);
            });

            #region AppUser Properties Configuration
            builder.Entity<AppUser>(entity =>
            {
                entity.Property(u => u.Id).HasMaxLength(36);
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.ProfilePicturePath).IsRequired().HasMaxLength(500);
                entity.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("EmailIndex").HasFilter("[NormalizedEmail] IS NOT NULL");
                entity.Property(u => u.PasswordResetTokenHash).HasMaxLength(64).IsUnicode(false);
            });
            #endregion

            builder.HasDefaultSchema("Identity");
            builder.Entity<AppUser>().ToTable("Users");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        }
    }
}
