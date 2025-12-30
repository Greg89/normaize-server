using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Normaize.DataNormalization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeSchemaToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DataSetRows_DataSets_DataSetId",
                schema: "DataNormalization",
                table: "DataSetRows");

            migrationBuilder.DropForeignKey(
                name: "FK_normalization_jobs_DataSets_dataset_id",
                schema: "data_normalization",
                table: "normalization_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DataSets",
                schema: "DataNormalization",
                table: "DataSets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DataSetRows",
                schema: "DataNormalization",
                table: "DataSetRows");

            migrationBuilder.RenameTable(
                name: "DataSets",
                schema: "DataNormalization",
                newName: "datasets",
                newSchema: "data_normalization");

            migrationBuilder.RenameTable(
                name: "DataSetRows",
                schema: "DataNormalization",
                newName: "dataset_rows",
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

            migrationBuilder.RenameColumn(
                name: "StorageProvider",
                schema: "data_normalization",
                table: "datasets",
                newName: "storage_provider");

            migrationBuilder.RenameColumn(
                name: "StatsUseSeparateTable",
                schema: "data_normalization",
                table: "datasets",
                newName: "stats_use_separate_table");

            migrationBuilder.RenameColumn(
                name: "StatsRowCount",
                schema: "data_normalization",
                table: "datasets",
                newName: "stats_row_count");

            migrationBuilder.RenameColumn(
                name: "StatsProcessedAt",
                schema: "data_normalization",
                table: "datasets",
                newName: "stats_processed_at");

            migrationBuilder.RenameColumn(
                name: "StatsIsProcessed",
                schema: "data_normalization",
                table: "datasets",
                newName: "stats_is_processed");

            migrationBuilder.RenameColumn(
                name: "StatsColumnCount",
                schema: "data_normalization",
                table: "datasets",
                newName: "stats_column_count");

            migrationBuilder.RenameColumn(
                name: "FileType",
                schema: "data_normalization",
                table: "datasets",
                newName: "file_type");

            migrationBuilder.RenameColumn(
                name: "FileSize",
                schema: "data_normalization",
                table: "datasets",
                newName: "file_size");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                schema: "data_normalization",
                table: "datasets",
                newName: "file_path");

            migrationBuilder.RenameColumn(
                name: "FileName",
                schema: "data_normalization",
                table: "datasets",
                newName: "file_name");

            migrationBuilder.RenameColumn(
                name: "DataHash",
                schema: "data_normalization",
                table: "datasets",
                newName: "data_hash");

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

            migrationBuilder.RenameIndex(
                name: "IX_DataSets_IsDeleted_UserId",
                schema: "data_normalization",
                table: "datasets",
                newName: "ix_datasets_is_deleted_user_id");

            migrationBuilder.RenameColumn(
                name: "Data",
                schema: "data_normalization",
                table: "dataset_rows",
                newName: "data");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "data_normalization",
                table: "dataset_rows",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "data_normalization",
                table: "dataset_rows",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "RowIndex",
                schema: "data_normalization",
                table: "dataset_rows",
                newName: "row_index");

            migrationBuilder.RenameColumn(
                name: "DataSetId",
                schema: "data_normalization",
                table: "dataset_rows",
                newName: "dataset_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "data_normalization",
                table: "dataset_rows",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_DataSetRows_DataSetId_RowIndex",
                schema: "data_normalization",
                table: "dataset_rows",
                newName: "ix_dataset_rows_dataset_id_row_index");

            migrationBuilder.RenameIndex(
                name: "IX_DataSetRows_DataSetId",
                schema: "data_normalization",
                table: "dataset_rows",
                newName: "ix_dataset_rows_dataset_id");

            migrationBuilder.RenameIndex(
                name: "IX_DataSetRows_CreatedAt",
                schema: "data_normalization",
                table: "dataset_rows",
                newName: "ix_dataset_rows_created_at");

            migrationBuilder.AddPrimaryKey(
                name: "PK_datasets",
                schema: "data_normalization",
                table: "datasets",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_dataset_rows",
                schema: "data_normalization",
                table: "dataset_rows",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_dataset_rows_datasets_dataset_id",
                schema: "data_normalization",
                table: "dataset_rows",
                column: "dataset_id",
                principalSchema: "data_normalization",
                principalTable: "datasets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_dataset_rows_datasets_dataset_id",
                schema: "data_normalization",
                table: "dataset_rows");

            migrationBuilder.DropForeignKey(
                name: "FK_normalization_jobs_datasets_dataset_id",
                schema: "data_normalization",
                table: "normalization_jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_datasets",
                schema: "data_normalization",
                table: "datasets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_dataset_rows",
                schema: "data_normalization",
                table: "dataset_rows");

            migrationBuilder.EnsureSchema(
                name: "DataNormalization");

            migrationBuilder.RenameTable(
                name: "datasets",
                schema: "data_normalization",
                newName: "DataSets",
                newSchema: "DataNormalization");

            migrationBuilder.RenameTable(
                name: "dataset_rows",
                schema: "data_normalization",
                newName: "DataSetRows",
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

            migrationBuilder.RenameColumn(
                name: "storage_provider",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "StorageProvider");

            migrationBuilder.RenameColumn(
                name: "stats_use_separate_table",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "StatsUseSeparateTable");

            migrationBuilder.RenameColumn(
                name: "stats_row_count",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "StatsRowCount");

            migrationBuilder.RenameColumn(
                name: "stats_processed_at",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "StatsProcessedAt");

            migrationBuilder.RenameColumn(
                name: "stats_is_processed",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "StatsIsProcessed");

            migrationBuilder.RenameColumn(
                name: "stats_column_count",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "StatsColumnCount");

            migrationBuilder.RenameColumn(
                name: "file_type",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "FileType");

            migrationBuilder.RenameColumn(
                name: "file_size",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "FileSize");

            migrationBuilder.RenameColumn(
                name: "file_path",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "FilePath");

            migrationBuilder.RenameColumn(
                name: "file_name",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "FileName");

            migrationBuilder.RenameColumn(
                name: "data_hash",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "DataHash");

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

            migrationBuilder.RenameIndex(
                name: "ix_datasets_is_deleted_user_id",
                schema: "DataNormalization",
                table: "DataSets",
                newName: "IX_DataSets_IsDeleted_UserId");

            migrationBuilder.RenameColumn(
                name: "data",
                schema: "DataNormalization",
                table: "DataSetRows",
                newName: "Data");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "DataNormalization",
                table: "DataSetRows",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "DataNormalization",
                table: "DataSetRows",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "row_index",
                schema: "DataNormalization",
                table: "DataSetRows",
                newName: "RowIndex");

            migrationBuilder.RenameColumn(
                name: "dataset_id",
                schema: "DataNormalization",
                table: "DataSetRows",
                newName: "DataSetId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "DataNormalization",
                table: "DataSetRows",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_dataset_rows_dataset_id_row_index",
                schema: "DataNormalization",
                table: "DataSetRows",
                newName: "IX_DataSetRows_DataSetId_RowIndex");

            migrationBuilder.RenameIndex(
                name: "ix_dataset_rows_dataset_id",
                schema: "DataNormalization",
                table: "DataSetRows",
                newName: "IX_DataSetRows_DataSetId");

            migrationBuilder.RenameIndex(
                name: "ix_dataset_rows_created_at",
                schema: "DataNormalization",
                table: "DataSetRows",
                newName: "IX_DataSetRows_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DataSets",
                schema: "DataNormalization",
                table: "DataSets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DataSetRows",
                schema: "DataNormalization",
                table: "DataSetRows",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DataSetRows_DataSets_DataSetId",
                schema: "DataNormalization",
                table: "DataSetRows",
                column: "DataSetId",
                principalSchema: "DataNormalization",
                principalTable: "DataSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
    }
}
