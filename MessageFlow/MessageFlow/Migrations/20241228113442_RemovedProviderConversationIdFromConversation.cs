using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Migrations
{
    /// <inheritdoc />
    public partial class RemovedProviderConversationIdFromConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderConversationId",
                table: "Conversations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderConversationId",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
