using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPhoto.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedTrialAndHasUsedTrialExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_used_trial_extension",
                table: "subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Rename plan string values to match the updated SubscriptionPlan enum
            migrationBuilder.Sql("UPDATE subscriptions SET plan = 'ExtendedTrial' WHERE plan = 'Starter'");
            migrationBuilder.Sql("UPDATE subscriptions SET plan = 'Premium' WHERE plan = 'Enterprise'");

            migrationBuilder.AlterColumn<string>(
                name: "moderation_status",
                table: "guest_uploads",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_used_trial_extension",
                table: "subscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "moderation_status",
                table: "guest_uploads",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
