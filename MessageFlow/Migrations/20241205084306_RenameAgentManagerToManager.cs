using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class RenameAgentManagerToManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "C12CCAC6-3668-4FDD-889A-C517F8E18D05",
                columns: new[] { "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[] { "Manager", "MANAGER", Guid.NewGuid().ToString() });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "C12CCAC6-3668-4FDD-889A-C517F8E18D05",
                columns: new[] { "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[] { "Agent Manager", "AGENT MANAGER", Guid.NewGuid().ToString() });
        }
    }
}
