using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Normaize.DataNormalization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class analysis_Add : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analyses",
                schema: "data_normalization",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    dataset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comparison_dataset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    configuration = table.Column<string>(type: "jsonb", nullable: true),
                    result = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analyses", x => x.id);
                    table.ForeignKey(
                        name: "FK_analyses_datasets_comparison_dataset_id",
                        column: x => x.comparison_dataset_id,
                        principalSchema: "data_normalization",
                        principalTable: "datasets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_analyses_datasets_dataset_id",
                        column: x => x.dataset_id,
                        principalSchema: "data_normalization",
                        principalTable: "datasets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analyses_comparison_dataset_id",
                schema: "data_normalization",
                table: "analyses",
                column: "comparison_dataset_id");

            migrationBuilder.CreateIndex(
                name: "ix_analyses_created_at",
                schema: "data_normalization",
                table: "analyses",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_analyses_dataset_id",
                schema: "data_normalization",
                table: "analyses",
                column: "dataset_id");

            migrationBuilder.CreateIndex(
                name: "ix_analyses_dataset_status",
                schema: "data_normalization",
                table: "analyses",
                columns: new[] { "dataset_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_analyses_is_deleted",
                schema: "data_normalization",
                table: "analyses",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_analyses_status",
                schema: "data_normalization",
                table: "analyses",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_analyses_status_type",
                schema: "data_normalization",
                table: "analyses",
                columns: new[] { "status", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_analyses_type",
                schema: "data_normalization",
                table: "analyses",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analyses",
                schema: "data_normalization");
        }
    }
}
