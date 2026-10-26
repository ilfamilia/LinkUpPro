using LinkUpPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Infrastructure.Persistence.EntityConfigurations
{
    public class BattleshipAttackEntityConfiguration : IEntityTypeConfiguration<BattleshipAttack>
    {
        public void Configure(EntityTypeBuilder<BattleshipAttack> builder)
        {
            #region Basic Configuration
            builder.HasKey(a => a.Id);
            builder.ToTable("BattleshipAttacks");
            #endregion

            #region Constraints Configuration
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_BattleshipAttacks_Position",
                "[Row] BETWEEN 0 AND 11 AND [Column] BETWEEN 0 AND 11"));
            #endregion

            #region Properties Configuration
            builder.Property(a => a.GameId).IsRequired();
            builder.Property(a => a.AttackerUserId).IsRequired().HasMaxLength(36);
            builder.Property(a => a.Row).IsRequired();
            builder.Property(a => a.Column).IsRequired();
            builder.Property(a => a.IsHit).IsRequired();
            builder.Property(a => a.AttackedAt).IsRequired();
            #endregion

            #region Indexes Configuration
            builder.HasIndex(a => new { a.GameId, a.AttackerUserId, a.Row, a.Column })
                .IsUnique()
                .HasDatabaseName("IX_BattleshipAttacks_NoRepeatedCell");

            builder.HasIndex(a => new { a.GameId, a.AttackedAt });
            #endregion
        }
    }
}
