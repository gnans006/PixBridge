using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace EventPhoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiDiscoveryEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "search_duration_ms",
                table: "guest_face_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "selfie_deleted_at",
                table: "guest_face_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selfie_hash",
                table: "guest_face_sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "embedding_version",
                table: "face_embeddings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "arcface-512-v1");

            migrationBuilder.AddColumn<int>(
                name: "face_count_in_photo",
                table: "face_embeddings",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<float>(
                name: "quality_score",
                table: "face_embeddings",
                type: "real",
                nullable: false,
                defaultValue: 50f);

            migrationBuilder.AddColumn<string>(
                name: "quality_tier",
                table: "face_embeddings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Medium");

            // Note: is_face_search_enabled and is_watermark_enabled are managed
            // by the AddFeatureFlags migration which runs before this one.

            migrationBuilder.CreateTable(
                name: "ai_search_analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    matches_found = table.Column<int>(type: "integer", nullable: false),
                    search_duration_ms = table.Column<int>(type: "integer", nullable: false),
                    was_successful = table.Column<bool>(type: "boolean", nullable: false),
                    top_match_category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    top_similarity_score = table.Column<float>(type: "real", nullable: true),
                    embedding_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    searched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_search_analytics", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_search_analytics_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "face_clusters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    representative_embedding = table.Column<Vector>(type: "vector(512)", nullable: false),
                    photo_count = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    average_quality_score = table.Column<float>(type: "real", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_face_clusters", x => x.id);
                    table.ForeignKey(
                        name: "FK_face_clusters_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "face_processing_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    failure_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    next_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_face_processing_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_face_processing_jobs_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_face_processing_jobs_photos_photo_id",
                        column: x => x.photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_guest_face_sessions_event_selfie_hash",
                table: "guest_face_sessions",
                columns: new[] { "event_id", "selfie_hash" });

            migrationBuilder.CreateIndex(
                name: "IX_guest_face_sessions_retention",
                table: "guest_face_sessions",
                columns: new[] { "expires_at", "selfie_deleted_at" },
                filter: "selfie_deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_face_embeddings_event_quality_tier",
                table: "face_embeddings",
                columns: new[] { "event_id", "quality_tier" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_search_analytics_event_searched_at",
                table: "ai_search_analytics",
                columns: new[] { "event_id", "searched_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_search_analytics_searched_at",
                table: "ai_search_analytics",
                column: "searched_at");

            migrationBuilder.CreateIndex(
                name: "IX_face_clusters_event_id",
                table: "face_clusters",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_face_processing_jobs_event_status",
                table: "face_processing_jobs",
                columns: new[] { "event_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_face_processing_jobs_pending_priority",
                table: "face_processing_jobs",
                columns: new[] { "status", "priority", "created_at" },
                filter: "status IN ('Pending', 'Queued')");

            migrationBuilder.CreateIndex(
                name: "IX_face_processing_jobs_photo_id",
                table: "face_processing_jobs",
                column: "photo_id");

            migrationBuilder.CreateIndex(
                name: "IX_face_processing_jobs_retry_eligible",
                table: "face_processing_jobs",
                columns: new[] { "status", "next_retry_at" },
                filter: "status = 'Failed'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_search_analytics");

            migrationBuilder.DropTable(
                name: "face_clusters");

            migrationBuilder.DropTable(
                name: "face_processing_jobs");

            migrationBuilder.DropIndex(
                name: "IX_guest_face_sessions_event_selfie_hash",
                table: "guest_face_sessions");

            migrationBuilder.DropIndex(
                name: "IX_guest_face_sessions_retention",
                table: "guest_face_sessions");

            migrationBuilder.DropIndex(
                name: "IX_face_embeddings_event_quality_tier",
                table: "face_embeddings");

            migrationBuilder.DropColumn(
                name: "search_duration_ms",
                table: "guest_face_sessions");

            migrationBuilder.DropColumn(
                name: "selfie_deleted_at",
                table: "guest_face_sessions");

            migrationBuilder.DropColumn(
                name: "selfie_hash",
                table: "guest_face_sessions");

            migrationBuilder.DropColumn(
                name: "embedding_version",
                table: "face_embeddings");

            migrationBuilder.DropColumn(
                name: "face_count_in_photo",
                table: "face_embeddings");

            migrationBuilder.DropColumn(
                name: "quality_score",
                table: "face_embeddings");

            migrationBuilder.DropColumn(
                name: "quality_tier",
                table: "face_embeddings");

            // Note: is_face_search_enabled and is_watermark_enabled belong to
            // the AddFeatureFlags migration — they are not dropped here.
        }
    }
}
