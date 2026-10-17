using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinkUpPro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FriendshipRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ReceiverUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsVisibleToSender = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FirstUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true, computedColumnSql: "CASE\r\n                        WHEN [SenderUserId] COLLATE Latin1_General_100_BIN2\r\n                           < [ReceiverUserId] COLLATE Latin1_General_100_BIN2\r\n                        THEN [SenderUserId]\r\n                        ELSE [ReceiverUserId]\r\n                      END", stored: true),
                    SecondUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true, computedColumnSql: "CASE\r\n                        WHEN [SenderUserId] COLLATE Latin1_General_100_BIN2\r\n                           < [ReceiverUserId] COLLATE Latin1_General_100_BIN2\r\n                        THEN [ReceiverUserId]\r\n                        ELSE [SenderUserId]\r\n                      END", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendshipRequests", x => x.Id);
                    table.CheckConstraint("CK_FriendshipRequests_DifferentUsers", "[SenderUserId] <> [ReceiverUserId]");
                    table.CheckConstraint("CK_FriendshipRequests_Status", "[Status] IN (N'Pending', N'Accepted', N'Rejected', N'Cancelled')");
                    table.CheckConstraint("CK_FriendshipRequests_StatusDates", "(\r\n                        ([Status] = N'Pending'\r\n                            AND [RespondedAt] IS NULL\r\n                            AND [CancelledAt] IS NULL)\r\n                        OR\r\n                        ([Status] IN (N'Accepted', N'Rejected')\r\n                            AND [RespondedAt] IS NOT NULL\r\n                            AND [CancelledAt] IS NULL)\r\n                        OR\r\n                        ([Status] = N'Cancelled'\r\n                            AND [RespondedAt] IS NULL\r\n                            AND [CancelledAt] IS NOT NULL)\r\n                    )");
                });

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    User1Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    User2Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => new { x.User1Id, x.User2Id });
                    table.CheckConstraint("CK_Friendships_Status", "[Status] IN (N'Active', N'Removed')");
                    table.CheckConstraint("CK_Friendships_UserOrder", "[User1Id] COLLATE Latin1_General_100_BIN2 < [User2Id] COLLATE Latin1_General_100_BIN2");
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Privacy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "FriendsOnly"),
                    MediaType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    YouTubeUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    AllowComments = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.CheckConstraint("CK_Posts_MediaContent", "(\r\n                        ([MediaType] = N'Image'\r\n                            AND [ImagePath] IS NOT NULL\r\n                            AND LTRIM(RTRIM([ImagePath])) <> N''\r\n                            AND [YouTubeUrl] IS NULL)\r\n                        OR\r\n                        ([MediaType] = N'YouTubeVideo'\r\n                            AND [YouTubeUrl] IS NOT NULL\r\n                            AND LTRIM(RTRIM([YouTubeUrl])) <> N''\r\n                            AND [ImagePath] IS NULL)\r\n                    )");
                    table.CheckConstraint("CK_Posts_MediaType", "[MediaType] IN (N'Image', N'YouTubeVideo')");
                    table.CheckConstraint("CK_Posts_Privacy", "[Privacy] IN (N'FriendsOnly', N'OnlyMe')");
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    ParentCommentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Comments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "Comments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Comments_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Reactions",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reactions", x => new { x.UserId, x.PostId });
                    table.CheckConstraint("CK_Reactions_Type", "[Type] IN (N'Like', N'Dislike')");
                    table.ForeignKey(
                        name: "FK_Reactions_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    RecipientUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    CommentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.CheckConstraint("CK_Notifications_DifferentUsers", "[ActorUserId] <> [RecipientUserId]");
                    table.CheckConstraint("CK_Notifications_Type", "[Type] IN (N'Comment', N'Reply', N'Reaction')");
                    table.ForeignKey(
                        name: "FK_Notifications_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_PostId",
                table: "Comments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_FriendshipRequests_Pending",
                table: "FriendshipRequests",
                columns: new[] { "FirstUserId", "SecondUserId" },
                unique: true,
                filter: "[Status] = N'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CommentId",
                table: "Notifications",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PostId",
                table: "Notifications",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_PostId",
                table: "Reactions",
                column: "PostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FriendshipRequests");

            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Reactions");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Posts");
        }
    }
}
