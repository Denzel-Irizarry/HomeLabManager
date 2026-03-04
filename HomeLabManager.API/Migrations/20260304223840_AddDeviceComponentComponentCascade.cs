using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeLabManager.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceComponentComponentCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DeviceComponents_ComponentId",
                table: "DeviceComponents",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceComponents_DeviceId_ComponentId",
                table: "DeviceComponents",
                columns: new[] { "DeviceId", "ComponentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Components_VendorId",
                table: "Components",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Components_Vendors_VendorId",
                table: "Components",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceComponents_Components_ComponentId",
                table: "DeviceComponents",
                column: "ComponentId",
                principalTable: "Components",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceComponents_Devices_DeviceId",
                table: "DeviceComponents",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Components_Vendors_VendorId",
                table: "Components");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceComponents_Components_ComponentId",
                table: "DeviceComponents");

            migrationBuilder.DropForeignKey(
                name: "FK_DeviceComponents_Devices_DeviceId",
                table: "DeviceComponents");

            migrationBuilder.DropIndex(
                name: "IX_DeviceComponents_ComponentId",
                table: "DeviceComponents");

            migrationBuilder.DropIndex(
                name: "IX_DeviceComponents_DeviceId_ComponentId",
                table: "DeviceComponents");

            migrationBuilder.DropIndex(
                name: "IX_Components_VendorId",
                table: "Components");
        }
    }
}
