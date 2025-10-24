using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normaize.DataNormalization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initial_ddd_migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "datasets",
                schema: "data_normalization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    schema = table.Column<string>(type: "jsonb", nullable: true),
                    preview_data = table.Column<string>(type: "jsonb", nullable: true),
                    processed_data = table.Column<string>(type: "jsonb", nullable: true),
                    processing_errors = table.Column<string>(type: "text", nullable: true),
                    retention_expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_datasets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "normalization_audit_logs",
                schema: "data_normalization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    normalization_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    changes = table.Column<string>(type: "jsonb", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_normalization_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_normalization_audit_logs_normalization_jobs_normalization_j~",
                        column: x => x.normalization_job_id,
                        principalSchema: "data_normalization",
                        principalTable: "normalization_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_datasets_soft_delete",
                schema: "data_normalization",
                table: "datasets",
                columns: new[] { "is_deleted", "deleted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_datasets_uploaded_at",
                schema: "data_normalization",
                table: "datasets",
                column: "uploaded_at");

            migrationBuilder.CreateIndex(
                name: "ix_datasets_user_id",
                schema: "data_normalization",
                table: "datasets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_action",
                schema: "data_normalization",
                table: "normalization_audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_job_id",
                schema: "data_normalization",
                table: "normalization_audit_logs",
                column: "normalization_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_job_timestamp",
                schema: "data_normalization",
                table: "normalization_audit_logs",
                columns: new[] { "normalization_job_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_timestamp",
                schema: "data_normalization",
                table: "normalization_audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                schema: "data_normalization",
                table: "normalization_audit_logs",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_normalization_jobs_datasets_dataset_id",
                schema: "data_normalization",
                table: "normalization_jobs",
                column: "dataset_id",
                principalSchema: "data_normalization",
                principalTable: "datasets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_normalization_jobs_datasets_dataset_id",
                schema: "data_normalization",
                table: "normalization_jobs");

            migrationBuilder.DropTable(
                name: "datasets",
                schema: "data_normalization");

            migrationBuilder.DropTable(
                name: "normalization_audit_logs",
                schema: "data_normalization");
        }
    }
}
