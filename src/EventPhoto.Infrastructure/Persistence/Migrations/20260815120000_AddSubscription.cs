#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EventPhoto.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phase 5 — Subscription Engine
    ///   - subscriptions  (singleton license/subscription record)
    /// </summary>
    public partial class AddSubscription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id                     = table.Column<Guid>(type: "uuid", nullable: false),
                    plan                   = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Trial"),
                    state                  = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Trial"),
                    license_key            = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    studio_email           = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    activated_at           = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at             = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    grace_period_ends_at   = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    max_events             = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    max_users_per_studio   = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    notes                  = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at             = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at             = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "subscriptions");
        }
    }
}
