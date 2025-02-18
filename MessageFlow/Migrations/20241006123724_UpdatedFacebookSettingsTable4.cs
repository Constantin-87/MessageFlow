using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedFacebookSettingsTable4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VerifyToken",
                table: "FacebookSettingsModels",
                newName: "WebhookVerifyToken");

            migrationBuilder.RenameColumn(
                name: "PageAccessToken",
                table: "FacebookSettingsModels",
                newName: "AccessToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WebhookVerifyToken",
                table: "FacebookSettingsModels",
                newName: "VerifyToken");

            migrationBuilder.RenameColumn(
                name: "AccessToken",
                table: "FacebookSettingsModels",
                newName: "PageAccessToken");
        }
    }
}
