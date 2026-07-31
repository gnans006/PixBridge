using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPhoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWatermarkRibbonFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "background_opacity",
                table: "watermark_configurations",
                type: "real",
                nullable: false,
                defaultValue: 0.20f);

            migrationBuilder.AddColumn<bool>(
                name: "apply_on_preview",
                table: "watermark_configurations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "background_opacity", table: "watermark_configurations");
            migrationBuilder.DropColumn(name: "apply_on_preview", table: "watermark_configurations");
        }
    }
}
