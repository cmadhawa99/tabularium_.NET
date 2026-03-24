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
                name: "file_records",
                columns: table => new
                {
                    rr_number = table.Column<string>(type: "text", nullable: false),
                    serial_number = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_file_records", x => x.rr_number);
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
                        name: "FK_borrow_records_file_records_file_rr_number",
                        column: x => x.file_rr_number,
                        principalTable: "file_records",
                        principalColumn: "rr_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_borrow_records_file_rr_number",
                table: "borrow_records",
                column: "file_rr_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "borrow_records");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "file_records");
        }
    }
}
