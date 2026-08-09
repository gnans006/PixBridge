using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPhoto.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddFeatureFlags : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_watermark_enabled",
            table: "application_settings",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_face_search_enabled",
            table: "application_settings",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "is_watermark_enabled",  table: "application_settings");
        migrationBuilder.DropColumn(name: "is_face_search_enabled", table: "application_settings");
    }
}
