using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Normaize.DataNormalization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatisticsEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Statistics",
                schema: "data_normalization",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DataSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSetName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    TotalColumns = table.Column<int>(type: "integer", nullable: false),
                    MissingValues = table.Column<int>(type: "integer", nullable: false),
                    DuplicateRows = table.Column<int>(type: "integer", nullable: false),
                    ColumnSummaries = table.Column<string>(type: "jsonb", nullable: false),
                    ColumnStatistics = table.Column<string>(type: "jsonb", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessingTime = table.Column<double>(type: "double precision", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statistics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_CalculatedAt",
                schema: "data_normalization",
                table: "Statistics",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_DataSetId",
                schema: "data_normalization",
                table: "Statistics",
                column: "DataSetId");

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_IsDeleted",
                schema: "data_normalization",
                table: "Statistics",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Statistics",
                schema: "data_normalization");
        }
    }
}
