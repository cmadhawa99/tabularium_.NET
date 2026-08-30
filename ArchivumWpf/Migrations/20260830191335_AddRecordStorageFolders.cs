using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchivumWpf.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordStorageFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "physical_storage_id",
                table: "folders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "record_storage_id",
                table: "digital_files",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "physical_storage_id",
                table: "folders");

            migrationBuilder.DropColumn(
                name: "record_storage_id",
                table: "digital_files");
        }
    }
}
