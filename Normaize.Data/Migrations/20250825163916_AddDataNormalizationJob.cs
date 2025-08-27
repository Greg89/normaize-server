using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normaize.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDataNormalizationJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataNormalizationJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataSetId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationParameters = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Results = table.Column<string>(type: "JSON", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxRetries = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    NextRetryAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CorrelationId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataNormalizationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataNormalizationJobs_DataSets_DataSetId",
                        column: x => x.DataSetId,
                        principalTable: "DataSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataNormalizationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NormalizationJobId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_DataNormalizationAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataNormalizationAuditLogs_DataNormalizationJobs_Normalizati~",
                        column: x => x.NormalizationJobId,
                        principalTable: "DataNormalizationJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_audit_job_timestamp",
                table: "DataNormalizationAuditLogs",
                columns: new[] { "NormalizationJobId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_DataNormalizationAuditLogs_Action",
                table: "DataNormalizationAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_DataNormalizationAuditLogs_NormalizationJobId",
                table: "DataNormalizationAuditLogs",
                column: "NormalizationJobId");

            migrationBuilder.CreateIndex(
                name: "IX_DataNormalizationAuditLogs_Timestamp",
                table: "DataNormalizationAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DataNormalizationAuditLogs_UserId",
                table: "DataNormalizationAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_jobs_soft_delete",
                table: "DataNormalizationJobs",
                columns: new[] { "IsDeleted", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "idx_jobs_status_priority",
                table: "DataNormalizationJobs",
                columns: new[] { "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_DataNormalizationJobs_DataSetId",
                table: "DataNormalizationJobs",
                column: "DataSetId");

            migrationBuilder.CreateIndex(
                name: "IX_DataNormalizationJobs_NextRetryAt",
                table: "DataNormalizationJobs",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataNormalizationJobs_Priority",
                table: "DataNormalizationJobs",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_DataNormalizationJobs_Status",
                table: "DataNormalizationJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DataNormalizationJobs_SubmittedAt",
                table: "DataNormalizationJobs",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataNormalizationJobs_UserId",
                table: "DataNormalizationJobs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataNormalizationAuditLogs");

            migrationBuilder.DropTable(
                name: "DataNormalizationJobs");
        }
    }
}
