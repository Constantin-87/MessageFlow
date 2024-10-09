using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedFacebookSettingsTable3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PageId",
                table: "FacebookSettingsModels",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PageId",
                table: "FacebookSettingsModels");
        }
    }
}
