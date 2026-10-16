using LinkUpPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Infrastructure.Persistence.EntityConfigurations
{
    public class ReactionEntityConfiguration : IEntityTypeConfiguration<Reaction>
    {
        public void Configure(EntityTypeBuilder<Reaction> builder)
        {
            #region Basic Configuration
            builder.HasKey(r => new { r.UserId, r.PostId });
            builder.ToTable("Reactions");
            #endregion

            #region Constraints Configuration
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Reactions_Type",
                "[Type] IN (N'Like', N'Dislike')"
            ));
            
            #endregion

            #region Properties Configuration
            builder.Property(r => r.UserId).IsRequired().HasMaxLength(36);
            builder.Property(r => r.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
            #endregion
        }
    }
}
