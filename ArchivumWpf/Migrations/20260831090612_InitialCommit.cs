using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArchivumWpf.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activity_log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SerialNumber = table.Column<string>(type: "text", nullable: false),
                    RrNumber = table.Column<string>(type: "text", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSecurityMetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EncryptedCanary = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSecurityMetas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "entry_history_record",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileSerialNumber = table.Column<int>(type: "integer", nullable: false),
                    RrNumber = table.Column<string>(type: "text", nullable: false),
                    SubjectNumber = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Sector = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalPages = table.Column<int>(type: "integer", nullable: true),
                    ShelfNumber = table.Column<string>(type: "text", nullable: true),
                    DeckNumber = table.Column<string>(type: "text", nullable: true),
                    FileNumber = table.Column<string>(type: "text", nullable: true),
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
                    shelf_number = table.Column<string>(type: "text", nullable: true),
                    deck_number = table.Column<string>(type: "text", nullable: true),
                    file_number = table.Column<string>(type: "text", nullable: true),
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
                    borrower_name = table.Column<string>(type: "text", nullable: false),
                    borrowed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    returned_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_returned = table.Column<bool>(type: "boolean", nullable: false),
                    snapshot_rr_number = table.Column<string>(type: "text", nullable: false),
                    snapshot_file_name = table.Column<string>(type: "text", nullable: false),
                    snapshot_sector = table.Column<string>(type: "text", nullable: false),
                    snapshot_sector_color = table.Column<string>(type: "text", nullable: true),
                    snapshot_subject_number = table.Column<string>(type: "text", nullable: true),
                    snapshot_file_type = table.Column<string>(type: "text", nullable: true),
                    snapshot_start_date = table.Column<DateTime>(type: "date", nullable: true),
                    snapshot_end_date = table.Column<DateTime>(type: "date", nullable: true),
                    snapshot_total_pages = table.Column<int>(type: "integer", nullable: true),
                    snapshot_shelf_number = table.Column<string>(type: "text", nullable: true),
                    snapshot_deck_number = table.Column<string>(type: "text", nullable: true),
                    snapshot_file_number = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "disposed_records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    file_serial_number = table.Column<int>(type: "integer", nullable: false),
                    reason_for_disposal = table.Column<string>(type: "text", nullable: false),
                    authorized_by = table.Column<string>(type: "text", nullable: false),
                    to_be_removed_date = table.Column<DateTime>(type: "date", nullable: true),
                    removed_date = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disposed_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_disposed_records_file_records_file_serial_number",
                        column: x => x.file_serial_number,
                        principalTable: "file_records",
                        principalColumn: "serial_number",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "folders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_folder_id = table.Column<int>(type: "integer", nullable: true),
                    folder_name = table.Column<string>(type: "text", nullable: false),
                    file_record_serial = table.Column<int>(type: "integer", nullable: false),
                    physical_storage_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_folders", x => x.id);
                    table.ForeignKey(
                        name: "FK_folders_file_records_file_record_serial",
                        column: x => x.file_record_serial,
                        principalTable: "file_records",
                        principalColumn: "serial_number",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_folders_folders_parent_folder_id",
                        column: x => x.parent_folder_id,
                        principalTable: "folders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "digital_files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    folder_id = table.Column<int>(type: "integer", nullable: false),
                    original_file_name = table.Column<string>(type: "text", nullable: false),
                    physical_file_name = table.Column<string>(type: "text", nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    encrypted_dek = table.Column<string>(type: "text", nullable: false),
                    iv = table.Column<string>(type: "text", nullable: false),
                    record_storage_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_digital_files", x => x.id);
                    table.ForeignKey(
                        name: "FK_digital_files_folders_folder_id",
                        column: x => x.folder_id,
                        principalTable: "folders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_borrow_records_file_serial_number",
                table: "borrow_records",
                column: "file_serial_number");

            migrationBuilder.CreateIndex(
                name: "IX_digital_files_folder_id",
                table: "digital_files",
                column: "folder_id");

            migrationBuilder.CreateIndex(
                name: "IX_disposed_records_file_serial_number",
                table: "disposed_records",
                column: "file_serial_number");

            migrationBuilder.CreateIndex(
                name: "IX_file_records_rr_number",
                table: "file_records",
                column: "rr_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_folders_file_record_serial",
                table: "folders",
                column: "file_record_serial");

            migrationBuilder.CreateIndex(
                name: "IX_folders_parent_folder_id",
                table: "folders",
                column: "parent_folder_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activity_log");

            migrationBuilder.DropTable(
                name: "AppSecurityMetas");

            migrationBuilder.DropTable(
                name: "borrow_records");

            migrationBuilder.DropTable(
                name: "digital_files");

            migrationBuilder.DropTable(
                name: "disposed_records");

            migrationBuilder.DropTable(
                name: "entry_history_record");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "folders");

            migrationBuilder.DropTable(
                name: "file_records");
        }
    }
}
