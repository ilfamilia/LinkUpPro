using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkUpPro.Infrastructure.Persistence.EntityConfigurations;

/// <summary>Enforces canonical active pairs, versioned game writes, and unique fleet/attack records.</summary>
public sealed class BattleshipGameEntityConfiguration : IEntityTypeConfiguration<BattleshipGame>
{
    /// <summary>Maps the complete game aggregate and its relational integrity constraints.</summary>
    public void Configure(EntityTypeBuilder<BattleshipGame> builder)
    {
        builder.ToTable("BattleshipGames", table =>
        {
            table.HasCheckConstraint("CK_BattleshipGames_UserOrder", "[User1Id] COLLATE Latin1_General_100_BIN2 < [User2Id] COLLATE Latin1_General_100_BIN2");
            table.HasCheckConstraint("CK_BattleshipGames_Creator", "[CreatorUserId] IN ([User1Id], [User2Id])");
            table.HasCheckConstraint("CK_BattleshipGames_State", "([Status] = 0 AND [TurnUserId] IS NULL AND [TurnAssignedAt] IS NULL AND [CompletedAt] IS NULL AND [WinnerUserId] IS NULL AND [EndReason] IS NULL) OR ([Status] = 1 AND [TurnUserId] IN ([User1Id], [User2Id]) AND [TurnUserId] IS NOT NULL AND [TurnAssignedAt] IS NOT NULL AND [CompletedAt] IS NULL AND [WinnerUserId] IS NULL AND [EndReason] IS NULL) OR ([Status] = 2 AND [TurnUserId] IS NULL AND [TurnAssignedAt] IS NULL AND [CompletedAt] IS NOT NULL AND [WinnerUserId] IN ([User1Id], [User2Id]) AND [WinnerUserId] IS NOT NULL AND [EndReason] IN (0, 1, 2) AND [EndReason] IS NOT NULL)");
        });
        builder.HasKey(g => g.Id);
        builder.Property(g => g.User1Id).HasMaxLength(36).IsRequired();
        builder.Property(g => g.User2Id).HasMaxLength(36).IsRequired();
        builder.Property(g => g.CreatorUserId).HasMaxLength(36).IsRequired();
        builder.Property(g => g.TurnUserId).HasMaxLength(36);
        builder.Property(g => g.WinnerUserId).HasMaxLength(36);
        builder.Property(g => g.Version).IsConcurrencyToken();
        builder.HasIndex(g => new { g.User1Id, g.User2Id }).IsUnique().HasFilter("[Status] <> 2");
        builder.HasMany(g => g.Ships).WithOne().HasForeignKey(s => s.GameId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(g => g.Attacks).WithOne().HasForeignKey(a => a.GameId).OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>Prevents duplicate ship numbers and invalid fleet sizes or starting coordinates.</summary>
public sealed class ShipPlacementEntityConfiguration : IEntityTypeConfiguration<ShipPlacement>
{
    /// <summary>Maps each participant's five individually identified ships.</summary>
    public void Configure(EntityTypeBuilder<ShipPlacement> builder)
    {
        builder.ToTable("ShipPlacements", table =>
        {
            table.HasCheckConstraint("CK_ShipPlacements_Coordinates", "[Row] BETWEEN 0 AND 11 AND [Column] BETWEEN 0 AND 11 AND [Direction] BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_ShipPlacements_Fleet", "([ShipNumber] = 0 AND [Size] = 2) OR ([ShipNumber] IN (1, 2) AND [Size] = 3) OR ([ShipNumber] = 3 AND [Size] = 4) OR ([ShipNumber] = 4 AND [Size] = 5)");
        });
        builder.HasKey(s => new { s.GameId, s.UserId, s.ShipNumber });
        builder.Property(s => s.UserId).HasMaxLength(36).IsRequired();
    }
}

/// <summary>Guarantees that each player can persist at most one attack per cell and game.</summary>
public sealed class AttackEntityConfiguration : IEntityTypeConfiguration<Attack>
{
    /// <summary>Maps attack history with a coordinate constraint and composite unique key.</summary>
    public void Configure(EntityTypeBuilder<Attack> builder)
    {
        builder.ToTable("Attacks", table => table.HasCheckConstraint("CK_Attacks_Coordinates", "[Row] BETWEEN 0 AND 11 AND [Column] BETWEEN 0 AND 11"));
        builder.HasKey(a => new { a.GameId, a.UserId, a.Row, a.Column });
        builder.Property(a => a.UserId).HasMaxLength(36).IsRequired();
    }
}
