using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPhoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StudioProfileAndBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "full_name",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "application_settings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "brand_theme",
                table: "application_settings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "dark");

            migrationBuilder.AddColumn<Guid>(
                name: "default_watermark_profile_id",
                table: "application_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "application_settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "facebook",
                table: "application_settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "instagram",
                table: "application_settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_path",
                table: "application_settings",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "application_settings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_color",
                table: "application_settings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#6366f1");

            migrationBuilder.AddColumn<string>(
                name: "secondary_color",
                table: "application_settings",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#8b5cf6");

            migrationBuilder.AddColumn<string>(
                name: "website",
                table: "application_settings",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "whats_app",
                table: "application_settings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_timestamp",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropColumn(
                name: "full_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "users");

            migrationBuilder.DropColumn(
                name: "address",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "brand_theme",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "default_watermark_profile_id",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "email",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "facebook",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "instagram",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "logo_path",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "primary_color",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "secondary_color",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "website",
                table: "application_settings");

            migrationBuilder.DropColumn(
                name: "whats_app",
                table: "application_settings");
        }
    }
}
