using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkUpPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBattleship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BattleshipGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    User1Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    User2Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    CreatorUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TurnUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    TurnAssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WinnerUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    EndReason = table.Column<int>(type: "int", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleshipGames", x => x.Id);
                    table.CheckConstraint("CK_BattleshipGames_Creator", "[CreatorUserId] IN ([User1Id], [User2Id])");
                    table.CheckConstraint("CK_BattleshipGames_State", "([Status] = 0 AND [TurnUserId] IS NULL AND [TurnAssignedAt] IS NULL AND [CompletedAt] IS NULL AND [WinnerUserId] IS NULL AND [EndReason] IS NULL) OR ([Status] = 1 AND [TurnUserId] IN ([User1Id], [User2Id]) AND [TurnUserId] IS NOT NULL AND [TurnAssignedAt] IS NOT NULL AND [CompletedAt] IS NULL AND [WinnerUserId] IS NULL AND [EndReason] IS NULL) OR ([Status] = 2 AND [TurnUserId] IS NULL AND [TurnAssignedAt] IS NULL AND [CompletedAt] IS NOT NULL AND [WinnerUserId] IN ([User1Id], [User2Id]) AND [WinnerUserId] IS NOT NULL AND [EndReason] IN (0, 1, 2) AND [EndReason] IS NOT NULL)");
                    table.CheckConstraint("CK_BattleshipGames_UserOrder", "[User1Id] COLLATE Latin1_General_100_BIN2 < [User2Id] COLLATE Latin1_General_100_BIN2");
                });

            migrationBuilder.CreateTable(
                name: "Attacks",
                columns: table => new
                {
                    GameId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Row = table.Column<int>(type: "int", nullable: false),
                    Column = table.Column<int>(type: "int", nullable: false),
                    IsHit = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attacks", x => new { x.GameId, x.UserId, x.Row, x.Column });
                    table.CheckConstraint("CK_Attacks_Coordinates", "[Row] BETWEEN 0 AND 11 AND [Column] BETWEEN 0 AND 11");
                    table.ForeignKey(
                        name: "FK_Attacks_BattleshipGames_GameId",
                        column: x => x.GameId,
                        principalTable: "BattleshipGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipPlacements",
                columns: table => new
                {
                    GameId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ShipNumber = table.Column<int>(type: "int", nullable: false),
                    Row = table.Column<int>(type: "int", nullable: false),
                    Column = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipPlacements", x => new { x.GameId, x.UserId, x.ShipNumber });
                    table.CheckConstraint("CK_ShipPlacements_Coordinates", "[Row] BETWEEN 0 AND 11 AND [Column] BETWEEN 0 AND 11 AND [Direction] BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_ShipPlacements_Fleet", "([ShipNumber] = 0 AND [Size] = 2) OR ([ShipNumber] IN (1, 2) AND [Size] = 3) OR ([ShipNumber] = 3 AND [Size] = 4) OR ([ShipNumber] = 4 AND [Size] = 5)");
                    table.ForeignKey(
                        name: "FK_ShipPlacements_BattleshipGames_GameId",
                        column: x => x.GameId,
                        principalTable: "BattleshipGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BattleshipGames_User1Id_User2Id",
                table: "BattleshipGames",
                columns: new[] { "User1Id", "User2Id" },
                unique: true,
                filter: "[Status] <> 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attacks");

            migrationBuilder.DropTable(
                name: "ShipPlacements");

            migrationBuilder.DropTable(
                name: "BattleshipGames");
        }
    }
}
