using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Normaize.DataNormalization.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertAnalysisIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop primary key constraint
            migrationBuilder.DropPrimaryKey(
                name: "PK_analyses",
                schema: "data_normalization",
                table: "analyses");

            // Step 2: Create new UUID column with default value
            migrationBuilder.AddColumn<Guid>(
                name: "id_new",
                schema: "data_normalization",
                table: "analyses",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            // Step 3: Drop old integer column
            migrationBuilder.DropColumn(
                name: "id",
                schema: "data_normalization",
                table: "analyses");

            // Step 4: Rename new column to 'id'
            migrationBuilder.RenameColumn(
                name: "id_new",
                schema: "data_normalization",
                table: "analyses",
                newName: "id");

            // Step 5: Add primary key constraint back
            migrationBuilder.AddPrimaryKey(
                name: "PK_analyses",
                schema: "data_normalization",
                table: "analyses",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // WARNING: This will lose all data - UUIDs cannot be converted back to sequential integers
            throw new NotSupportedException(
                "Cannot downgrade from Guid to int without data loss. " +
                "Restore from backup if rollback is needed.");
        }
    }
}
