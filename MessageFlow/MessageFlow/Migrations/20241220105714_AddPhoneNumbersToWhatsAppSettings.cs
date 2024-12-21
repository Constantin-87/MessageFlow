using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneNumbersToWhatsAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhoneNumberId",
                table: "WhatsAppSettingsModels",
                newName: "BusinessAccountId");

            migrationBuilder.CreateTable(
                name: "PhoneNumberInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumberId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumberDesc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WhatsAppSettingsModelId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumberInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneNumberInfo_WhatsAppSettingsModels_WhatsAppSettingsModelId",
                        column: x => x.WhatsAppSettingsModelId,
                        principalTable: "WhatsAppSettingsModels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumberInfo_WhatsAppSettingsModelId",
                table: "PhoneNumberInfo",
                column: "WhatsAppSettingsModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneNumberInfo");

            migrationBuilder.RenameColumn(
                name: "BusinessAccountId",
                table: "WhatsAppSettingsModels",
                newName: "PhoneNumberId");
        }
    }
}
