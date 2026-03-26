using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArchivumWpf.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "entry_history_record",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileSerialNumber = table.Column<int>(type: "integer", nullable: false),
                    RrNumber = table.Column<string>(type: "text", nullable: false),
                    SubjectNumber = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Sector = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalPages = table.Column<int>(type: "integer", nullable: true),
                    ShelfNumber = table.Column<int>(type: "integer", nullable: true),
                    DeckNumber = table.Column<int>(type: "integer", nullable: true),
                    FileNumber = table.Column<int>(type: "integer", nullable: true),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entry_history_record", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "file_records",
                columns: table => new
                {
                    serial_number = table.Column<int>(type: "integer", nullable: false),
                    rr_number = table.Column<string>(type: "text", nullable: false),
                    sector = table.Column<string>(type: "text", nullable: false),
                    subject_number = table.Column<string>(type: "text", nullable: true),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    file_type = table.Column<string>(type: "text", nullable: true),
                    start_date = table.Column<DateTime>(type: "date", nullable: true),
                    end_date = table.Column<DateTime>(type: "date", nullable: true),
                    total_pages = table.Column<int>(type: "integer", nullable: true),
                    shelf_number = table.Column<int>(type: "integer", nullable: true),
                    deck_number = table.Column<int>(type: "integer", nullable: true),
                    file_number = table.Column<int>(type: "integer", nullable: true),
                    current_status = table.Column<string>(type: "text", nullable: false),
                    to_be_removed_date = table.Column<DateTime>(type: "date", nullable: true),
                    removed_date = table.Column<DateTime>(type: "date", nullable: true),
                    is_removed = table.Column<bool>(type: "boolean", nullable: false),
                    AddedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_records", x => x.serial_number);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    totp_secret = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "borrow_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    file_serial_number = table.Column<int>(type: "integer", nullable: false),
                    file_rr_number = table.Column<string>(type: "text", nullable: false),
                    borrower_name = table.Column<string>(type: "text", nullable: false),
                    borrowed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    returned_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_returned = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_borrow_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_borrow_records_file_records_file_serial_number",
                        column: x => x.file_serial_number,
                        principalTable: "file_records",
                        principalColumn: "serial_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_borrow_records_file_serial_number",
                table: "borrow_records",
                column: "file_serial_number");

            migrationBuilder.CreateIndex(
                name: "IX_file_records_rr_number",
                table: "file_records",
                column: "rr_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "borrow_records");

            migrationBuilder.DropTable(
                name: "entry_history_record");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "file_records");
        }
    }
}
