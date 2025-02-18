using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Server.Migrations
{
    /// <inheritdoc />
    public partial class reconfiguredPhoneNumbersInWhatsAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhoneNumberInfo_WhatsAppSettingsModels_WhatsAppSettingsModelId",
                table: "PhoneNumberInfo");

            migrationBuilder.AlterColumn<int>(
                name: "WhatsAppSettingsModelId",
                table: "PhoneNumberInfo",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneNumberInfo_WhatsAppSettingsModels_WhatsAppSettingsModelId",
                table: "PhoneNumberInfo",
                column: "WhatsAppSettingsModelId",
                principalTable: "WhatsAppSettingsModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhoneNumberInfo_WhatsAppSettingsModels_WhatsAppSettingsModelId",
                table: "PhoneNumberInfo");

            migrationBuilder.AlterColumn<int>(
                name: "WhatsAppSettingsModelId",
                table: "PhoneNumberInfo",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneNumberInfo_WhatsAppSettingsModels_WhatsAppSettingsModelId",
                table: "PhoneNumberInfo",
                column: "WhatsAppSettingsModelId",
                principalTable: "WhatsAppSettingsModels",
                principalColumn: "Id");
        }
    }
}
