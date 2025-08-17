using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normaize.Data.Migrations
{
    /// <inheritdoc />
    public partial class Retention_Days_Adjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetentionDays",
                table: "DataSets");

            migrationBuilder.AddColumn<int>(
                name: "RetentionDays",
                table: "UserSettings",
                type: "int",
                nullable: false,
                defaultValue: 365);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetentionDays",
                table: "UserSettings");

            migrationBuilder.AddColumn<int>(
                name: "RetentionDays",
                table: "DataSets",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
