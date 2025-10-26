using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normaize.DataNormalization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingStatusAndRetentionPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "retention_expiry_date",
                schema: "data_normalization",
                table: "datasets",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When this dataset should be automatically deleted");

            migrationBuilder.AddColumn<string>(
                name: "processing_error",
                schema: "data_normalization",
                table: "datasets",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "processing_is_processed",
                schema: "data_normalization",
                table: "datasets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "processing_processed_at",
                schema: "data_normalization",
                table: "datasets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retention_days",
                schema: "data_normalization",
                table: "datasets",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "processing_error",
                schema: "data_normalization",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "processing_is_processed",
                schema: "data_normalization",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "processing_processed_at",
                schema: "data_normalization",
                table: "datasets");

            migrationBuilder.DropColumn(
                name: "retention_days",
                schema: "data_normalization",
                table: "datasets");

            migrationBuilder.AlterColumn<DateTime>(
                name: "retention_expiry_date",
                schema: "data_normalization",
                table: "datasets",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When this dataset should be automatically deleted",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
