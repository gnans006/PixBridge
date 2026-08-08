using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPhoto.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddApplicationSettings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "application_settings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                studio_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                server_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                public_base_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                server_port = table.Column<int>(type: "integer", nullable: false),
                default_event_gallery_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                enable_watermark_by_default = table.Column<bool>(type: "boolean", nullable: false),
                enable_face_recognition_by_default = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_application_settings", x => x.id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "application_settings");
    }
}
