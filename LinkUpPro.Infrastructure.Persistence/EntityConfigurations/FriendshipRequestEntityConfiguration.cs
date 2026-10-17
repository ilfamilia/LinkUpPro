using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Infrastructure.Persistence.EntityConfigurations
{
    public class FriendshipRequestEntityConfiguration : IEntityTypeConfiguration<FriendshipRequest>
    {
        public void Configure(EntityTypeBuilder<FriendshipRequest> builder)
        {
            #region Basic Configuration
            builder.HasKey(fr => fr.Id);
            builder.ToTable("FriendshipRequests");
            #endregion

            #region Constraints Configuration
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_FriendshipRequests_Status",
                    "[Status] IN (N'Pending', N'Accepted', N'Rejected', N'Cancelled')"
                );

                t.HasCheckConstraint(
                    "CK_FriendshipRequests_DifferentUsers",
                    "[SenderUserId] <> [ReceiverUserId]"
                );

                t.HasCheckConstraint(
                    "CK_FriendshipRequests_StatusDates",
                    @"(
                        ([Status] = N'Pending'
                            AND [RespondedAt] IS NULL
                            AND [CancelledAt] IS NULL)
                        OR
                        ([Status] IN (N'Accepted', N'Rejected')
                            AND [RespondedAt] IS NOT NULL
                            AND [CancelledAt] IS NULL)
                        OR
                        ([Status] = N'Cancelled'
                            AND [RespondedAt] IS NULL
                            AND [CancelledAt] IS NOT NULL)
                    )"
                );
            });
            #endregion

            #region Properties Configuration
            builder.Property(fr => fr.SenderUserId).IsRequired().HasMaxLength(36);
            builder.Property(fr => fr.ReceiverUserId).IsRequired().HasMaxLength(36);
            builder.Property(fr => fr.RequestedAt).IsRequired();
            builder.Property(fr => fr.IsVisibleToSender).IsRequired().HasDefaultValue(true);
            builder.Property(fr => fr.Status).IsRequired().HasConversion<string>().HasMaxLength(20).HasDefaultValue(FriendshipRequestStatus.Pending);
            #endregion

            #region Indexes Configuration
            builder.Property<string>("FirstUserId")
                .HasMaxLength(36)
                .HasComputedColumnSql(
                    @"CASE
                        WHEN [SenderUserId] COLLATE Latin1_General_100_BIN2
                           < [ReceiverUserId] COLLATE Latin1_General_100_BIN2
                        THEN [SenderUserId]
                        ELSE [ReceiverUserId]
                      END",
                    stored: true
                );

            builder.Property<string>("SecondUserId")
                .HasMaxLength(36)
                .HasComputedColumnSql(
                    @"CASE
                        WHEN [SenderUserId] COLLATE Latin1_General_100_BIN2
                           < [ReceiverUserId] COLLATE Latin1_General_100_BIN2
                        THEN [ReceiverUserId]
                        ELSE [SenderUserId]
                      END",
                    stored: true
                );

            builder.HasIndex("FirstUserId", "SecondUserId")
                .IsUnique()
                .HasDatabaseName("IX_FriendshipRequests_Pending")
                .HasFilter("[Status] = N'Pending'");
            #endregion
        }
    }
}
