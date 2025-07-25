using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normaize.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailNotificationsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    PushNotificationsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ProcessingCompleteNotifications = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ErrorNotifications = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    WeeklyDigestEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Theme = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "light")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Language = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "en")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultPageSize = table.Column<int>(type: "int", nullable: false, defaultValue: 20),
                    ShowTutorials = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CompactMode = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    AutoProcessUploads = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    MaxPreviewRows = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    DefaultFileType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "CSV")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EnableDataValidation = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    EnableSchemaInference = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ShareAnalytics = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    AllowDataUsageForImprovement = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    ShowProcessingTime = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DisplayName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeZone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, defaultValue: "UTC")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateFormat = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true, defaultValue: "MM/dd/yyyy")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeFormat = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true, defaultValue: "12h")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_IsDeleted",
                table: "UserSettings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UpdatedAt",
                table: "UserSettings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");
        }
    }
}
