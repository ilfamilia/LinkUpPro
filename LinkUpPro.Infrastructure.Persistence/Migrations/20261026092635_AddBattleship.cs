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
                    CreatorUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    OpponentUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentTurnUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    WinnerUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    EndReason = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BattleStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TurnStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    FirstUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true, computedColumnSql: "CASE\r\n                        WHEN [CreatorUserId] COLLATE Latin1_General_100_BIN2\r\n                           < [OpponentUserId] COLLATE Latin1_General_100_BIN2\r\n                        THEN [CreatorUserId]\r\n                        ELSE [OpponentUserId]\r\n                      END", stored: true),
                    SecondUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true, computedColumnSql: "CASE\r\n                        WHEN [CreatorUserId] COLLATE Latin1_General_100_BIN2\r\n                           < [OpponentUserId] COLLATE Latin1_General_100_BIN2\r\n                        THEN [OpponentUserId]\r\n                        ELSE [CreatorUserId]\r\n                      END", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleshipGames", x => x.Id);
                    table.CheckConstraint("CK_BattleshipGames_CurrentTurnPlayer", "[CurrentTurnUserId] IS NULL OR [CurrentTurnUserId] IN ([CreatorUserId], [OpponentUserId])");
                    table.CheckConstraint("CK_BattleshipGames_DifferentPlayers", "[CreatorUserId] <> [OpponentUserId]");
                    table.CheckConstraint("CK_BattleshipGames_EndReason", "[EndReason] IS NULL OR [EndReason] IN (N'AllShipsSunk', N'Surrender', N'Abandonment')");
                    table.CheckConstraint("CK_BattleshipGames_Status", "[Status] IN (N'SettingUp', N'InProgress', N'Finished')");
                    table.CheckConstraint("CK_BattleshipGames_StatusFields", "(\r\n                        ([Status] = N'SettingUp'\r\n                            AND [CurrentTurnUserId] IS NULL\r\n                            AND [WinnerUserId] IS NULL\r\n                            AND [EndReason] IS NULL\r\n                            AND [BattleStartedAt] IS NULL\r\n                            AND [TurnStartedAt] IS NULL\r\n                            AND [FinishedAt] IS NULL)\r\n                        OR\r\n                        ([Status] = N'InProgress'\r\n                            AND [CurrentTurnUserId] IS NOT NULL\r\n                            AND [WinnerUserId] IS NULL\r\n                            AND [EndReason] IS NULL\r\n                            AND [BattleStartedAt] IS NOT NULL\r\n                            AND [TurnStartedAt] IS NOT NULL\r\n                            AND [FinishedAt] IS NULL)\r\n                        OR\r\n                        ([Status] = N'Finished'\r\n                            AND [CurrentTurnUserId] IS NULL\r\n                            AND [WinnerUserId] IS NOT NULL\r\n                            AND [EndReason] IS NOT NULL\r\n                            AND [TurnStartedAt] IS NULL\r\n                            AND [FinishedAt] IS NOT NULL)\r\n                    )");
                    table.CheckConstraint("CK_BattleshipGames_WinnerPlayer", "[WinnerUserId] IS NULL OR [WinnerUserId] IN ([CreatorUserId], [OpponentUserId])");
                });

            migrationBuilder.CreateTable(
                name: "BattleshipAttacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    AttackerUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Row = table.Column<int>(type: "int", nullable: false),
                    Column = table.Column<int>(type: "int", nullable: false),
                    IsHit = table.Column<bool>(type: "bit", nullable: false),
                    AttackedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleshipAttacks", x => x.Id);
                    table.CheckConstraint("CK_BattleshipAttacks_Position", "[Row] BETWEEN 0 AND 11 AND [Column] BETWEEN 0 AND 11");
                    table.ForeignKey(
                        name: "FK_BattleshipAttacks_BattleshipGames_GameId",
                        column: x => x.GameId,
                        principalTable: "BattleshipGames",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BattleshipShips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ShipNumber = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<int>(type: "int", nullable: false),
                    StartRow = table.Column<int>(type: "int", nullable: false),
                    StartColumn = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleshipShips", x => x.Id);
                    table.CheckConstraint("CK_BattleshipShips_Direction", "[Direction] IN (N'Up', N'Down', N'Left', N'Right')");
                    table.CheckConstraint("CK_BattleshipShips_Position", "[StartRow] BETWEEN 0 AND 11 AND [StartColumn] BETWEEN 0 AND 11");
                    table.CheckConstraint("CK_BattleshipShips_ShipComposition", "([ShipNumber] = 1 AND [Size] = 2)\r\n                      OR ([ShipNumber] IN (2, 3) AND [Size] = 3)\r\n                      OR ([ShipNumber] = 4 AND [Size] = 4)\r\n                      OR ([ShipNumber] = 5 AND [Size] = 5)");
                    table.CheckConstraint("CK_BattleshipShips_WithinBoard", "([Direction] = N'Up'    AND [StartRow]    - [Size] + 1 >= 0)\r\n                      OR ([Direction] = N'Down'  AND [StartRow]    + [Size] - 1 <= 11)\r\n                      OR ([Direction] = N'Left'  AND [StartColumn] - [Size] + 1 >= 0)\r\n                      OR ([Direction] = N'Right' AND [StartColumn] + [Size] - 1 <= 11)");
                    table.ForeignKey(
                        name: "FK_BattleshipShips_BattleshipGames_GameId",
                        column: x => x.GameId,
                        principalTable: "BattleshipGames",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BattleshipAttacks_GameId_AttackedAt",
                table: "BattleshipAttacks",
                columns: new[] { "GameId", "AttackedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BattleshipAttacks_NoRepeatedCell",
                table: "BattleshipAttacks",
                columns: new[] { "GameId", "AttackerUserId", "Row", "Column" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BattleshipGames_ActivePair",
                table: "BattleshipGames",
                columns: new[] { "FirstUserId", "SecondUserId" },
                unique: true,
                filter: "[Status] IN (N'SettingUp', N'InProgress')");

            migrationBuilder.CreateIndex(
                name: "IX_BattleshipGames_CreatorUserId_Status",
                table: "BattleshipGames",
                columns: new[] { "CreatorUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BattleshipGames_OpponentUserId_Status",
                table: "BattleshipGames",
                columns: new[] { "OpponentUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BattleshipShips_GameUserShipNumber",
                table: "BattleshipShips",
                columns: new[] { "GameId", "UserId", "ShipNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BattleshipAttacks");

            migrationBuilder.DropTable(
                name: "BattleshipShips");

            migrationBuilder.DropTable(
                name: "BattleshipGames");
        }
    }
}
