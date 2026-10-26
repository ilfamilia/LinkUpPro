using LinkUpPro.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Infrastructure.Persistence.EntityConfigurations
{
    public class BattleshipGameEntityConfiguration : IEntityTypeConfiguration<BattleshipGame>
    {
        public void Configure(EntityTypeBuilder<BattleshipGame> builder)
        {
            #region Basic Configuration
            builder.HasKey(g => g.Id);
            builder.ToTable("BattleshipGames");
            #endregion

            #region Constraints Configuration
            builder.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_BattleshipGames_DifferentPlayers",
                    "[CreatorUserId] <> [OpponentUserId]");

                t.HasCheckConstraint(
                    "CK_BattleshipGames_Status",
                    "[Status] IN (N'SettingUp', N'InProgress', N'Finished')");

                t.HasCheckConstraint(
                    "CK_BattleshipGames_EndReason",
                    "[EndReason] IS NULL OR " +
                    "[EndReason] IN (N'AllShipsSunk', N'Surrender', N'Abandonment')");

                t.HasCheckConstraint(
                    "CK_BattleshipGames_CurrentTurnPlayer",
                    "[CurrentTurnUserId] IS NULL OR " +
                    "[CurrentTurnUserId] IN ([CreatorUserId], [OpponentUserId])");

                t.HasCheckConstraint(
                    "CK_BattleshipGames_WinnerPlayer",
                    "[WinnerUserId] IS NULL OR " +
                    "[WinnerUserId] IN ([CreatorUserId], [OpponentUserId])");

                t.HasCheckConstraint(
                    "CK_BattleshipGames_StatusFields",
                    @"(
                        ([Status] = N'SettingUp'
                            AND [CurrentTurnUserId] IS NULL
                            AND [WinnerUserId] IS NULL
                            AND [EndReason] IS NULL
                            AND [BattleStartedAt] IS NULL
                            AND [TurnStartedAt] IS NULL
                            AND [FinishedAt] IS NULL)
                        OR
                        ([Status] = N'InProgress'
                            AND [CurrentTurnUserId] IS NOT NULL
                            AND [WinnerUserId] IS NULL
                            AND [EndReason] IS NULL
                            AND [BattleStartedAt] IS NOT NULL
                            AND [TurnStartedAt] IS NOT NULL
                            AND [FinishedAt] IS NULL)
                        OR
                        ([Status] = N'Finished'
                            AND [CurrentTurnUserId] IS NULL
                            AND [WinnerUserId] IS NOT NULL
                            AND [EndReason] IS NOT NULL
                            AND [TurnStartedAt] IS NULL
                            AND [FinishedAt] IS NOT NULL)
                    )");
            });
            #endregion

            #region Properties Configuration
            builder.Property(g => g.CreatorUserId).IsRequired().HasMaxLength(36);
            builder.Property(g => g.OpponentUserId).IsRequired().HasMaxLength(36);
            builder.Property(g => g.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
            builder.Property(g => g.CurrentTurnUserId).HasMaxLength(36);
            builder.Property(g => g.WinnerUserId).HasMaxLength(36);
            builder.Property(g => g.EndReason).HasConversion<string>().HasMaxLength(20);
            builder.Property(g => g.CreatedAt).IsRequired();
            builder.Property(g => g.RowVersion).IsRowVersion();
            #endregion

            #region Indexes Configuration
            builder.Property<string>("FirstUserId")
                .HasMaxLength(36)
                .HasComputedColumnSql(
                    @"CASE
                        WHEN [CreatorUserId] COLLATE Latin1_General_100_BIN2
                           < [OpponentUserId] COLLATE Latin1_General_100_BIN2
                        THEN [CreatorUserId]
                        ELSE [OpponentUserId]
                      END",
                    stored: true);

            builder.Property<string>("SecondUserId")
                .HasMaxLength(36)
                .HasComputedColumnSql(
                    @"CASE
                        WHEN [CreatorUserId] COLLATE Latin1_General_100_BIN2
                           < [OpponentUserId] COLLATE Latin1_General_100_BIN2
                        THEN [OpponentUserId]
                        ELSE [CreatorUserId]
                      END",
                    stored: true);

            builder.HasIndex("FirstUserId", "SecondUserId")
                .IsUnique()
                .HasDatabaseName("IX_BattleshipGames_ActivePair")
                .HasFilter("[Status] IN (N'SettingUp', N'InProgress')");

            builder.HasIndex(g => new { g.CreatorUserId, g.Status });
            builder.HasIndex(g => new { g.OpponentUserId, g.Status });
            #endregion

            #region Relationships Configuration
            builder.HasMany(g => g.Ships)
                .WithOne(s => s.Game)
                .HasForeignKey(s => s.GameId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(g => g.Attacks)
                .WithOne(a => a.Game)
                .HasForeignKey(a => a.GameId)
                .OnDelete(DeleteBehavior.NoAction);
            #endregion
        }
    }
}
