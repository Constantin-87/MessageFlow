using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedTeamIdToConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedTeamId",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedTeamId",
                table: "Conversations");
        }
    }
}
