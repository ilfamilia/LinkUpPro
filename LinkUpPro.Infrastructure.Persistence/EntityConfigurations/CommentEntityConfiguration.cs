using LinkUpPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Infrastructure.Persistence.EntityConfigurations
{
    public class CommentEntityConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            #region Basic Configuration
            builder.HasKey(c => c.Id);
            builder.ToTable("Comments");
            #endregion

            #region Properties Configuration
            builder.Property(c => c.UserId).IsRequired().HasMaxLength(36);
            builder.Property(c => c.Content).IsRequired().HasMaxLength(500);
            builder.Property(c => c.CreatedAt).IsRequired();
            builder.Property(c => c.UpdatedAt).IsRequired(false);
            builder.Property(c => c.IsDeleted).HasDefaultValue(false);
            #endregion

            #region Relationships Configuration
            builder.HasMany(c => c.Replies)
                .WithOne(c => c.ParentComment)
                .HasForeignKey(c => c.ParentCommentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion
        }
    }
}
