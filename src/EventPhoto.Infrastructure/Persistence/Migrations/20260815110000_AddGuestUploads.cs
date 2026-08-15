#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EventPhoto.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phase 4 — Guest Experience Platform
    ///   - guest_upload_sessions  (per-event upload sessions with session codes)
    ///   - guest_uploads          (guest-submitted photos awaiting moderation)
    /// </summary>
    public partial class AddGuestUploads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guest_upload_sessions",
                columns: table => new
                {
                    id              = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id        = table.Column<Guid>(type: "uuid", nullable: false),
                    session_code    = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    title           = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    photo_count     = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status          = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    closed_at       = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at      = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guest_upload_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_guest_upload_sessions_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guest_uploads",
                columns: table => new
                {
                    id                 = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id           = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id         = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    stored_path        = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    thumbnail_path     = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    file_size_bytes    = table.Column<long>(type: "bigint", nullable: false),
                    content_type       = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: "image/jpeg"),
                    uploaded_at        = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    moderation_status  = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    rejection_reason   = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at         = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at         = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guest_uploads", x => x.id);
                    table.ForeignKey(
                        name: "FK_guest_uploads_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_guest_uploads_guest_upload_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "guest_upload_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guest_upload_sessions_code",
                table: "guest_upload_sessions",
                column: "session_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guest_upload_sessions_event_id",
                table: "guest_upload_sessions",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_guest_uploads_event_id",
                table: "guest_uploads",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_guest_uploads_session_id",
                table: "guest_uploads",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_guest_uploads_event_moderation",
                table: "guest_uploads",
                columns: new[] { "event_id", "moderation_status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "guest_uploads");
            migrationBuilder.DropTable(name: "guest_upload_sessions");
        }
    }
}
