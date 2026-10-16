using LinkUpPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Infrastructure.Persistence.EntityConfigurations
{
    public class FriendshipEntityConfiguration : IEntityTypeConfiguration<Friendship>
    {
        public void Configure(EntityTypeBuilder<Friendship> builder)
        {
            #region Basic Configuration
            builder.HasKey(f => new { f.User1Id, f.User2Id });

            builder.ToTable("Friendships", table =>
            {
                table.HasCheckConstraint(
                    "CK_Friendships_UserOrder",
                    "[User1Id] COLLATE Latin1_General_100_BIN2 < " +
                    "[User2Id] COLLATE Latin1_General_100_BIN2"
                );

                table.HasCheckConstraint(
                    "CK_Friendships_Status",
                    "[Status] IN (N'Active', N'Removed')"
                );
            });
            #endregion

            #region Properties Configuration
            builder.Property(f => f.User1Id).IsRequired().HasMaxLength(36);
            builder.Property(f => f.User2Id).IsRequired().HasMaxLength(36);
            builder.Property(f => f.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            #endregion
        }
    }
}
