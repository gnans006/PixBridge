using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPhoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    event_type = table.Column<int>(type: "integer", nullable: false),
                    event_date = table.Column<DateOnly>(type: "date", nullable: false),
                    venue_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    client_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    watch_folder = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    thumbnail_folder = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    qr_code_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    qr_code_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    photo_count = table.Column<int>(type: "integer", nullable: false),
                    total_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    original_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    thumbnail_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    taken_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    download_count = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    thumbnail_status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photos", x => x.id);
                    table.ForeignKey(
                        name: "FK_photos_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "download_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    downloaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_download_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_download_logs_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_download_logs_photos_photo_id",
                        column: x => x.photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_download_logs_downloaded_at",
                table: "download_logs",
                column: "downloaded_at");

            migrationBuilder.CreateIndex(
                name: "IX_download_logs_event_id",
                table: "download_logs",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_download_logs_photo_id",
                table: "download_logs",
                column: "photo_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_is_active_is_deleted",
                table: "events",
                columns: new[] { "is_active", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_events_is_deleted",
                table: "events",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_photos_event_id",
                table: "photos",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_photos_original_path",
                table: "photos",
                column: "original_path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_photos_thumbnail_status_is_deleted",
                table: "photos",
                columns: new[] { "thumbnail_status", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_system_settings_key",
                table: "system_settings",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "download_logs");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "photos");

            migrationBuilder.DropTable(
                name: "events");
        }
    }
}
