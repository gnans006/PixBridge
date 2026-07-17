using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPhoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceRecognitionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "face_index_retry_count",
                table: "photos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "face_index_status",
                table: "photos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "allow_face_search",
                table: "events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_gallery_browsing",
                table: "events",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "enable_face_recognition",
                table: "events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<float>(
                name: "face_match_threshold",
                table: "events",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<bool>(
                name: "restrict_downloads_to_matched_photos",
                table: "events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_photos_face_index_status",
                table: "photos",
                columns: new[] { "face_index_status", "is_deleted" },
                filter: "face_index_status = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_photos_face_index_status",
                table: "photos");

            migrationBuilder.DropColumn(
                name: "face_index_retry_count",
                table: "photos");

            migrationBuilder.DropColumn(
                name: "face_index_status",
                table: "photos");

            migrationBuilder.DropColumn(
                name: "allow_face_search",
                table: "events");

            migrationBuilder.DropColumn(
                name: "allow_gallery_browsing",
                table: "events");

            migrationBuilder.DropColumn(
                name: "enable_face_recognition",
                table: "events");

            migrationBuilder.DropColumn(
                name: "face_match_threshold",
                table: "events");

            migrationBuilder.DropColumn(
                name: "restrict_downloads_to_matched_photos",
                table: "events");
        }
    }
}
