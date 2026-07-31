using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPhoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWatermarkConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "watermark_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    style = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    opacity = table.Column<float>(type: "real", nullable: false),
                    scale = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    custom_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    template = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    logo_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    include_studio_name = table.Column<bool>(type: "boolean", nullable: false),
                    include_event_name = table.Column<bool>(type: "boolean", nullable: false),
                    include_download_date = table.Column<bool>(type: "boolean", nullable: false),
                    apply_on_download = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watermark_configurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_watermark_configurations_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_watermark_configurations_event_id",
                table: "watermark_configurations",
                column: "event_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "watermark_configurations");
        }
    }
}
