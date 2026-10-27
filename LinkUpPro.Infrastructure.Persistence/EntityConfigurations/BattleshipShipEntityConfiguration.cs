using LinkUpPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Infrastructure.Persistence.EntityConfigurations
{
    public class BattleshipShipEntityConfiguration : IEntityTypeConfiguration<BattleshipShip>
    {
        public void Configure(EntityTypeBuilder<BattleshipShip> builder)
        {
            #region Basic Configuration
            builder.HasKey(s => s.Id);
            builder.ToTable("BattleshipShips");
            #endregion

            #region Constraints Configuration
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_BattleshipShips_ShipComposition",
                    @"([ShipNumber] = 1 AND [Size] = 2)
                      OR ([ShipNumber] IN (2, 3) AND [Size] = 3)
                      OR ([ShipNumber] = 4 AND [Size] = 4)
                      OR ([ShipNumber] = 5 AND [Size] = 5)");

                t.HasCheckConstraint(
                    "CK_BattleshipShips_Position",
                    "[StartRow] BETWEEN 0 AND 11 AND [StartColumn] BETWEEN 0 AND 11");

                t.HasCheckConstraint(
                    "CK_BattleshipShips_Direction",
                    "[Direction] IN (N'Up', N'Down', N'Left', N'Right')");

                t.HasCheckConstraint(
                    "CK_BattleshipShips_WithinBoard",
                    @"([Direction] = N'Up'    AND [StartRow]    - [Size] + 1 >= 0)
                      OR ([Direction] = N'Down'  AND [StartRow]    + [Size] - 1 <= 11)
                      OR ([Direction] = N'Left'  AND [StartColumn] - [Size] + 1 >= 0)
                      OR ([Direction] = N'Right' AND [StartColumn] + [Size] - 1 <= 11)");
            });
            #endregion

            #region Properties Configuration
            builder.Property(s => s.GameId).IsRequired();
            builder.Property(s => s.UserId).IsRequired().HasMaxLength(36);
            builder.Property(s => s.ShipNumber).IsRequired();
            builder.Property(s => s.Size).IsRequired();
            builder.Property(s => s.StartRow).IsRequired();
            builder.Property(s => s.StartColumn).IsRequired();
            builder.Property(s => s.Direction).IsRequired().HasConversion<string>().HasMaxLength(20);
            #endregion

            #region Indexes Configuration
            builder.HasIndex(s => new { s.GameId, s.UserId, s.ShipNumber })
                .IsUnique()
                .HasDatabaseName("IX_BattleshipShips_GameUserShipNumber");
            #endregion
        }
    }
}