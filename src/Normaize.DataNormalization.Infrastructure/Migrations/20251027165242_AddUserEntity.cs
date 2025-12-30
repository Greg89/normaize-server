using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normaize.DataNormalization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                schema: "data_normalization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Auth0UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Preferences_Theme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Preferences_Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Preferences_TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Preferences_DateFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Preferences_TimeFormat = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Preferences_DefaultPageSize = table.Column<int>(type: "integer", nullable: false),
                    Preferences_ShowTutorials = table.Column<bool>(type: "boolean", nullable: false),
                    Preferences_CompactMode = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSettings_EmailNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSettings_PushNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSettings_ProcessingCompleteNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSettings_ErrorNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSettings_WeeklyDigestEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingDefaults_AutoProcessUploads = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingDefaults_MaxPreviewRows = table.Column<int>(type: "integer", nullable: false),
                    ProcessingDefaults_DefaultFileType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcessingDefaults_EnableDataValidation = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingDefaults_EnableSchemaInference = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingDefaults_RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    PrivacySettings_ShareAnalytics = table.Column<bool>(type: "boolean", nullable: false),
                    PrivacySettings_AllowDataUsageForImprovement = table.Column<bool>(type: "boolean", nullable: false),
                    PrivacySettings_ShowProcessingTime = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Auth0UserId",
                schema: "data_normalization",
                table: "Users",
                column: "Auth0UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                schema: "data_normalization",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDeleted",
                schema: "data_normalization",
                table: "Users",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users",
                schema: "data_normalization");
        }
    }
}
