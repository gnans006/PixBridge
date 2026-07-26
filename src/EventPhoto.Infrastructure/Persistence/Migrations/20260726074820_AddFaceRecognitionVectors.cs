using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace EventPhoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceRecognitionVectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "face_embeddings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(512)", nullable: false),
                    bounding_box = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    confidence = table.Column<float>(type: "real", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_face_embeddings", x => x.id);
                    table.ForeignKey(
                        name: "FK_face_embeddings_photos_photo_id",
                        column: x => x.photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guest_face_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    selfie_embedding = table.Column<Vector>(type: "vector(512)", nullable: false),
                    search_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    search_completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    match_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guest_face_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_guest_face_sessions_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "photo_matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    similarity_score = table.Column<float>(type: "real", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_matches", x => x.id);
                    table.ForeignKey(
                        name: "FK_photo_matches_guest_face_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "guest_face_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_photo_matches_photos_photo_id",
                        column: x => x.photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_face_embeddings_event_id",
                table: "face_embeddings",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_face_embeddings_photo_id",
                table: "face_embeddings",
                column: "photo_id");

            migrationBuilder.CreateIndex(
                name: "IX_guest_face_sessions_event_status",
                table: "guest_face_sessions",
                columns: new[] { "event_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_guest_face_sessions_expires_at",
                table: "guest_face_sessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_guest_face_sessions_token",
                table: "guest_face_sessions",
                column: "session_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_photo_matches_photo_id",
                table: "photo_matches",
                column: "photo_id");

            migrationBuilder.CreateIndex(
                name: "IX_photo_matches_session_photo",
                table: "photo_matches",
                columns: new[] { "session_id", "photo_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_photo_matches_session_score",
                table: "photo_matches",
                columns: new[] { "session_id", "similarity_score" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "face_embeddings");

            migrationBuilder.DropTable(
                name: "photo_matches");

            migrationBuilder.DropTable(
                name: "guest_face_sessions");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
