using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveConversationSchemaUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArchivedMessages_ArchivedConversations_ArchivedConversationId",
                table: "ArchivedMessages");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "ArchivedConversations",
                newName: "CompanyId");

            migrationBuilder.RenameColumn(
                name: "ArchivedAt",
                table: "ArchivedConversations",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<string>(
                name: "ArchivedConversationId",
                table: "ArchivedMessages",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ArchivedMessages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignedUserId",
                table: "ArchivedConversations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_ArchivedMessages_ArchivedConversations_ArchivedConversationId",
                table: "ArchivedMessages",
                column: "ArchivedConversationId",
                principalTable: "ArchivedConversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArchivedMessages_ArchivedConversations_ArchivedConversationId",
                table: "ArchivedMessages");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ArchivedMessages");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "ArchivedConversations");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ArchivedConversations",
                newName: "ArchivedAt");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "ArchivedConversations",
                newName: "Title");

            migrationBuilder.AlterColumn<string>(
                name: "ArchivedConversationId",
                table: "ArchivedMessages",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_ArchivedMessages_ArchivedConversations_ArchivedConversationId",
                table: "ArchivedMessages",
                column: "ArchivedConversationId",
                principalTable: "ArchivedConversations",
                principalColumn: "Id");
        }
    }
}
