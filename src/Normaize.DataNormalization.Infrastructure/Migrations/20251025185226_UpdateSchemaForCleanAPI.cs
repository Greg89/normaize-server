using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normaize.DataNormalization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchemaForCleanAPI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_normalization_jobs_datasets_dataset_id",
                schema: "data_normalization",
                table: "normalization_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_datasets",
                schema: "data_normalization",
                table: "datasets");

            migrationBuilder.DropIndex(
                name: "ix_datasets_soft_delete",
                schema: "data_normalization",
                table: "datasets");

            migrationBuilder.EnsureSchema(
                name: "DataNormalization");

            migrationBuilder.RenameTable(
                name: "datasets",
                schema: "data_normalization",
                newName: "DataSets",
                newSchema: "DataNormalization");

            migrationBuilder.RenameColumn(
                name: "schema",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "Schema");

            migrationBuilder.RenameColumn(
                name: "name",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "uploaded_at",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "UploadedAt");

            migrationBuilder.RenameColumn(
                name: "retention_expiry_date",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "RetentionExpiryDate");

            migrationBuilder.RenameColumn(
                name: "processing_errors",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "ProcessingErrors");

            migrationBuilder.RenameColumn(
                name: "processed_data",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "ProcessedData");

            migrationBuilder.RenameColumn(
                name: "preview_data",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "PreviewData");

            migrationBuilder.RenameColumn(
                name: "last_modified_by",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "LastModifiedBy");

            migrationBuilder.RenameColumn(
                name: "last_modified_at",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "LastModifiedAt");

            migrationBuilder.RenameColumn(
                name: "is_deleted",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "deleted_by",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "DeletedBy");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "DeletedAt");

            migrationBuilder.RenameIndex(
                name: "ix_datasets_user_id",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "IX_DataSets_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_datasets_uploaded_at",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "IX_DataSets_UploadedAt");

            migrationBuilder.AlterColumn<string>(
                name: "Schema",
                schema: "DataNormalization",
                table: "DataSets",
                type: "jsonb",
                nullable: true,
                comment: "JSON schema definition for the dataset",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "DataNormalization",
                table: "DataSets",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                comment: "Human-readable name of the dataset",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "DataNormalization",
                table: "DataSets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                comment: "Optional description of the dataset",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "DataNormalization",
                table: "DataSets",
                type: "uuid",
                nullable: false,
                comment: "Unique identifier for the dataset",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "uuid_generate_v4()");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "DataNormalization",
                table: "DataSets",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                comment: "ID of the user who owns this dataset",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UploadedAt",
                schema: "DataNormalization",
                table: "DataSets",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the dataset was uploaded",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RetentionExpiryDate",
                schema: "DataNormalization",
                table: "DataSets",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When this dataset should be automatically deleted",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProcessingErrors",
                schema: "DataNormalization",
                table: "DataSets",
                type: "text",
                nullable: true,
                comment: "Any errors encountered during processing",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProcessedData",
                schema: "DataNormalization",
                table: "DataSets",
                type: "jsonb",
                nullable: true,
                comment: "Processed data for small datasets",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PreviewData",
                schema: "DataNormalization",
                table: "DataSets",
                type: "jsonb",
                nullable: true,
                comment: "Sample data for preview purposes",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastModifiedBy",
                schema: "DataNormalization",
                table: "DataSets",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                comment: "Who last modified the dataset",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                schema: "DataNormalization",
                table: "DataSets",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the dataset was last modified",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "DataNormalization",
                table: "DataSets",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Whether this dataset has been soft deleted",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "DeletedBy",
                schema: "DataNormalization",
                table: "DataSets",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                comment: "Who deleted the dataset",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAt",
                schema: "DataNormalization",
                table: "DataSets",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the dataset was deleted",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataHash",
                schema: "DataNormalization",
                table: "DataSets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                schema: "DataNormalization",
                table: "DataSets",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                schema: "DataNormalization",
                table: "DataSets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                schema: "DataNormalization",
                table: "DataSets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                schema: "DataNormalization",
                table: "DataSets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StatsColumnCount",
                schema: "DataNormalization",
                table: "DataSets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "StatsIsProcessed",
                schema: "DataNormalization",
                table: "DataSets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatsProcessedAt",
                schema: "DataNormalization",
                table: "DataSets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatsRowCount",
                schema: "DataNormalization",
                table: "DataSets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "StatsUseSeparateTable",
                schema: "DataNormalization",
                table: "DataSets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StorageProvider",
                schema: "DataNormalization",
                table: "DataSets",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DataSets",
                schema: "DataNormalization",
                table: "DataSets",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "DataSetRows",
                schema: "DataNormalization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Unique identifier for the row"),
                    DataSetId = table.Column<Guid>(type: "uuid", nullable: false, comment: "Foreign key to the parent dataset"),
                    RowIndex = table.Column<int>(type: "integer", nullable: false, comment: "Zero-based index of this row within the dataset"),
                    Data = table.Column<string>(type: "jsonb", nullable: false, comment: "JSON representation of the row data"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "When this row was created"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "When this row was last updated")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSetRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataSetRows_DataSets_DataSetId",
                        column: x => x.DataSetId,
                        principalSchema: "DataNormalization",
                        principalTable: "DataSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataSets_IsDeleted_UserId",
                schema: "DataNormalization",
                table: "DataSets",
                columns: new[] { "IsDeleted", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DataSetRows_CreatedAt",
                schema: "DataNormalization",
                table: "DataSetRows",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataSetRows_DataSetId",
                schema: "DataNormalization",
                table: "DataSetRows",
                column: "DataSetId");

            migrationBuilder.CreateIndex(
                name: "IX_DataSetRows_DataSetId_RowIndex",
                schema: "DataNormalization",
                table: "DataSetRows",
                columns: new[] { "DataSetId", "RowIndex" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_normalization_jobs_DataSets_dataset_id",
                schema: "data_normalization",
                table: "normalization_jobs",
                column: "dataset_id",
                principalSchema: "DataNormalization",
                principalTable: "DataSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_normalization_jobs_DataSets_dataset_id",
                schema: "data_normalization",
                table: "normalization_jobs");

            migrationBuilder.DropTable(
                name: "DataSetRows",
                schema: "DataNormalization");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DataSets",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropIndex(
                name: "IX_DataSets_IsDeleted_UserId",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "DataHash",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "FilePath",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "FileSize",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "FileType",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "StatsColumnCount",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "StatsIsProcessed",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "StatsProcessedAt",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "StatsRowCount",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "StatsUseSeparateTable",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropColumn(
                name: "StorageProvider",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.RenameTable(
                name: "DataSets",
                schema: "DataNormalization",
                newName: "datasets",
                newSchema: "data_normalization");

            migrationBuilder.RenameColumn(
                name: "Schema",
                schema: "data_normalization",
                table: "datasets",
                newName: "schema");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "data_normalization",
                table: "datasets",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                schema: "data_normalization",
                table: "datasets",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "data_normalization",
                table: "datasets",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "data_normalization",
                table: "datasets",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                schema: "data_normalization",
                table: "datasets",
                newName: "uploaded_at");

            migrationBuilder.RenameColumn(
                name: "RetentionExpiryDate",
                schema: "data_normalization",
                table: "datasets",
                newName: "retention_expiry_date");

            migrationBuilder.RenameColumn(
                name: "ProcessingErrors",
                schema: "data_normalization",
                table: "datasets",
                newName: "processing_errors");

            migrationBuilder.RenameColumn(
                name: "ProcessedData",
                schema: "data_normalization",
                table: "datasets",
                newName: "processed_data");

            migrationBuilder.RenameColumn(
                name: "PreviewData",
                schema: "data_normalization",
                table: "datasets",
                newName: "preview_data");

            migrationBuilder.RenameColumn(
                name: "LastModifiedBy",
                schema: "data_normalization",
                table: "datasets",
                newName: "last_modified_by");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                schema: "data_normalization",
                table: "datasets",
                newName: "last_modified_at");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                schema: "data_normalization",
                table: "datasets",
                newName: "is_deleted");

            migrationBuilder.RenameColumn(
                name: "DeletedBy",
                schema: "data_normalization",
                table: "datasets",
                newName: "deleted_by");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                schema: "data_normalization",
                table: "datasets",
                newName: "deleted_at");

            migrationBuilder.RenameIndex(
                name: "IX_DataSets_UserId",
                schema: "data_normalization",
                table: "datasets",
                newName: "ix_datasets_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_DataSets_UploadedAt",
                schema: "data_normalization",
                table: "datasets",
                newName: "ix_datasets_uploaded_at");

            migrationBuilder.AlterColumn<string>(
                name: "schema",
                schema: "data_normalization",
                table: "datasets",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "JSON schema definition for the dataset");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "data_normalization",
                table: "datasets",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldComment: "Human-readable name of the dataset");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "data_normalization",
                table: "datasets",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true,
                oldComment: "Optional description of the dataset");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "data_normalization",
                table: "datasets",
                type: "uuid",
                nullable: false,
                defaultValueSql: "uuid_generate_v4()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Unique identifier for the dataset");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                schema: "data_normalization",
                table: "datasets",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldComment: "ID of the user who owns this dataset");

            migrationBuilder.AlterColumn<DateTime>(
                name: "uploaded_at",
                schema: "data_normalization",
                table: "datasets",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "When the dataset was uploaded");

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

            migrationBuilder.AlterColumn<string>(
                name: "processing_errors",
                schema: "data_normalization",
                table: "datasets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Any errors encountered during processing");

            migrationBuilder.AlterColumn<string>(
                name: "processed_data",
                schema: "data_normalization",
                table: "datasets",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "Processed data for small datasets");

            migrationBuilder.AlterColumn<string>(
                name: "preview_data",
                schema: "data_normalization",
                table: "datasets",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "Sample data for preview purposes");

            migrationBuilder.AlterColumn<string>(
                name: "last_modified_by",
                schema: "data_normalization",
                table: "datasets",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true,
                oldComment: "Who last modified the dataset");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified_at",
                schema: "data_normalization",
                table: "datasets",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldComment: "When the dataset was last modified");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "data_normalization",
                table: "datasets",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Whether this dataset has been soft deleted");

            migrationBuilder.AlterColumn<string>(
                name: "deleted_by",
                schema: "data_normalization",
                table: "datasets",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true,
                oldComment: "Who deleted the dataset");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "data_normalization",
                table: "datasets",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the dataset was deleted");

            migrationBuilder.AddPrimaryKey(
                name: "PK_datasets",
                schema: "data_normalization",
                table: "datasets",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_datasets_soft_delete",
                schema: "data_normalization",
                table: "datasets",
                columns: new[] { "is_deleted", "deleted_at" });

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
    }
}
