using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normaize.DataNormalization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "data_normalization");

            migrationBuilder.CreateTable(
                name: "normalization_jobs",
                schema: "data_normalization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    dataset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    operation_parameters = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_retries = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    result = table.Column<string>(type: "jsonb", nullable: true),
                    progress_percentage = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    progress_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_normalization_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_normalization_jobs_created_at",
                schema: "data_normalization",
                table: "normalization_jobs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_normalization_jobs_dataset_id",
                schema: "data_normalization",
                table: "normalization_jobs",
                column: "dataset_id");

            migrationBuilder.CreateIndex(
                name: "ix_normalization_jobs_status",
                schema: "data_normalization",
                table: "normalization_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_normalization_jobs_status_created_at",
                schema: "data_normalization",
                table: "normalization_jobs",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "normalization_jobs",
                schema: "data_normalization");
        }
    }
}
