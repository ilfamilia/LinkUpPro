using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Infrastructure.Persistence.EntityConfigurations
{
    public class PostEntityConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            #region Basic Configuration
            builder.HasKey(p => p.Id);
            builder.ToTable("Posts");
            #endregion

            #region Constraints Configuration
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Posts_Privacy",
                    "[Privacy] IN (N'FriendsOnly', N'OnlyMe')"
                );

                t.HasCheckConstraint(
                    "CK_Posts_MediaType",
                    "[MediaType] IN (N'Image', N'YouTubeVideo')"
                );

                t.HasCheckConstraint(
                    "CK_Posts_MediaContent",
                    @"(
                        ([MediaType] = N'Image'
                            AND [ImagePath] IS NOT NULL
                            AND LTRIM(RTRIM([ImagePath])) <> N''
                            AND [YouTubeUrl] IS NULL)
                        OR
                        ([MediaType] = N'YouTubeVideo'
                            AND [YouTubeUrl] IS NOT NULL
                            AND LTRIM(RTRIM([YouTubeUrl])) <> N''
                            AND [ImagePath] IS NULL)
                    )"
                );
            });
            #endregion

            #region Properties Configuration
            builder.Property(p => p.UserId).IsRequired().HasMaxLength(36);
            builder.Property(p => p.Content).IsRequired().HasMaxLength(1000);
            builder.Property(n => n.Privacy).IsRequired().HasConversion<string>().HasMaxLength(20).HasDefaultValue(PostPrivacy.FriendsOnly); ;
            builder.Property(n => n.MediaType).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(p => p.ImagePath).HasMaxLength(500);
            builder.Property(p => p.YouTubeUrl).HasMaxLength(2048);
            builder.Property(n => n.AllowComments).IsRequired().HasDefaultValue(true);
            builder.Property(n => n.CreatedAt).IsRequired();
            builder.Property(n => n.IsDeleted).HasDefaultValue(false);
            #endregion

            #region Relationships Configuration
            builder.HasMany(p => p.Comments)
                .WithOne(c => c.Post)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(p => p.Notifications)
                .WithOne(n => n.Post)
                .HasForeignKey(n => n.PostId)
                .IsRequired()
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(p => p.Reactions)
                .WithOne(r => r.Post)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion
        }
    }
}
