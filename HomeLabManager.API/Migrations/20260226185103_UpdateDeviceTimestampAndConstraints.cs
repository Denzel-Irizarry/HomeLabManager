using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeLabManager.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeviceTimestampAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Products_ProductId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Vendors_VendorId",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Devices",
                newName: "CreatedAtUtc");

            migrationBuilder.AddColumn<string>(
                name: "VendorBaseUrl",
                table: "Vendors",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerialNumber",
                table: "Devices",
                type: "TEXT",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_SerialNumber",
                table: "Devices",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Products_ProductId",
                table: "Devices",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Vendors_VendorId",
                table: "Products",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Products_ProductId",
                table: "Devices");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Vendors_VendorId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Devices_SerialNumber",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "VendorBaseUrl",
                table: "Vendors");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Devices",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<string>(
                name: "SerialNumber",
                table: "Devices",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Products_ProductId",
                table: "Devices",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Vendors_VendorId",
                table: "Products",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
