using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normaize.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTrailAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DataSets",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "DataSets",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DataSets",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "DataSets",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "DataSets",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Analyses",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Analyses",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Analyses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DataSetAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DataSetId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Changes = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSetAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSetAuditLogs_DataSets_DataSetId",
                        column: x => x.DataSetId,
                        principalTable: "DataSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_datasets_soft_delete",
                table: "DataSets",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "idx_analyses_soft_delete",
                table: "Analyses",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_dataset_timestamp",
                table: "DataSetAuditLogs",
                columns: new[] { "DataSetId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_DataSetAuditLogs_Action",
                table: "DataSetAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_DataSetAuditLogs_DataSetId",
                table: "DataSetAuditLogs",
                column: "DataSetId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSetAuditLogs_Timestamp",
                table: "DataSetAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DataSetAuditLogs_UserId",
                table: "DataSetAuditLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataSetAuditLogs");

            migrationBuilder.DropIndex(
                name: "idx_datasets_soft_delete",
                table: "DataSets");

            migrationBuilder.DropIndex(
                name: "idx_analyses_soft_delete",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Analyses");
        }
    }
}
