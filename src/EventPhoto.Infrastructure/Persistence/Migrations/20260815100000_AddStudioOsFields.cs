#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace EventPhoto.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Studio OS Gap Fills:
    ///   - application_settings.gst_number   (Phase 1 — Studio Profile)
    ///   - application_settings.gallery_theme (Phase 2 — Branding)
    ///   - application_settings.qr_theme      (Phase 2 — Branding)
    /// </summary>
    public partial class AddStudioOsFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Gap 1 — GST Number on Studio Profile
            migrationBuilder.AddColumn<string>(
                name: "gst_number",
                table: "application_settings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Gap 3 — Gallery Theme
            migrationBuilder.AddColumn<string>(
                name: "gallery_theme",
                table: "application_settings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "minimal");

            // Gap 3 — QR Theme
            migrationBuilder.AddColumn<string>(
                name: "qr_theme",
                table: "application_settings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "standard");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "gst_number",     table: "application_settings");
            migrationBuilder.DropColumn(name: "gallery_theme",  table: "application_settings");
            migrationBuilder.DropColumn(name: "qr_theme",       table: "application_settings");
        }
    }
}
