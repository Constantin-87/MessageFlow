using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class PretrainDataFileModelChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FileType",
                table: "PretrainDataFiles",
                newName: "FileDescription");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FileDescription",
                table: "PretrainDataFiles",
                newName: "FileType");
        }
    }
}
