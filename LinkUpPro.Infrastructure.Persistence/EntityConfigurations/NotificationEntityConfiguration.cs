using LinkUpPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace LinkUpPro.Infrastructure.Persistence.EntityConfigurations
{
    public class NotificationEntityConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            #region Basic Configuration
            builder.HasKey(n => n.Id);
            builder.ToTable("Notifications");
            #endregion

            #region Constraints Configuration
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Notifications_Type",
                    "[Type] IN (N'Comment', N'Reply', N'Reaction')"
                );

                t.HasCheckConstraint(
                    "CK_Notifications_DifferentUsers",
                    "[ActorUserId] <> [RecipientUserId]"
                );
            });
            #endregion

            #region Properties Configuration
            builder.Property(n => n.ActorUserId).IsRequired().HasMaxLength(36);
            builder.Property(n => n.RecipientUserId).IsRequired().HasMaxLength(36);
            builder.Property(n => n.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(n => n.Description).IsRequired().HasMaxLength(500);
            builder.Property(n => n.CreatedAt).IsRequired();
            builder.Property(n => n.IsRead).HasDefaultValue(false);
            builder.Property(n => n.PostId).IsRequired();
            builder.Property(n => n.CommentId).IsRequired(false);
            #endregion
        }
    }
}
