using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchivumWpf.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "file_rr_number",
                table: "borrow_records",
                newName: "snapshot_sector");

            migrationBuilder.AddColumn<int>(
                name: "snapshot_deck_number",
                table: "borrow_records",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "snapshot_end_date",
                table: "borrow_records",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_file_name",
                table: "borrow_records",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "snapshot_file_number",
                table: "borrow_records",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_file_type",
                table: "borrow_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_rr_number",
                table: "borrow_records",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "snapshot_shelf_number",
                table: "borrow_records",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "snapshot_start_date",
                table: "borrow_records",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "snapshot_subject_number",
                table: "borrow_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "snapshot_total_pages",
                table: "borrow_records",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "snapshot_deck_number",
                table: "borrow_records");

            migrationBuilder.DropColumn(
                name: "snapshot_end_date",
                table: "borrow_records");

            migrationBuilder.DropColumn(
                name: "snapshot_file_name",
                table: "borrow_records");

            migrationBuilder.DropColumn(
                name: "snapshot_file_number",
                table: "borrow_records");

            migrationBuilder.DropColumn(
                name: "snapshot_file_type",
                table: "borrow_records");

            migrationBuilder.DropColumn(
                name: "snapshot_rr_number",
                table: "borrow_records");

            migrationBuilder.DropColumn(
                name: "snapshot_shelf_number",
                table: "borrow_records");

            migrationBuilder.DropColumn(
                name: "snapshot_start_date",
                table: "borrow_records");

            migrationBuilder.DropColumn(
                name: "snapshot_subject_number",
                table: "borrow_records");

            migrationBuilder.DropColumn(
                name: "snapshot_total_pages",
                table: "borrow_records");

            migrationBuilder.RenameColumn(
                name: "snapshot_sector",
                table: "borrow_records",
                newName: "file_rr_number");
        }
    }
}
