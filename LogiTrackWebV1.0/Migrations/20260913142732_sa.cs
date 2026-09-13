using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiTrackWebV1._0.Migrations
{
    /// <inheritdoc />
    public partial class sa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_AspNetUsers_UserId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Warehouses_WarehouseId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DriverLicense",
                table: "DriverLicense");

            migrationBuilder.DropIndex(
                name: "IX_DriverLicense_DriverId",
                table: "DriverLicense");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DriverLicense",
                table: "DriverLicense",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLicense_LicenseNumber",
                table: "DriverLicense",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Warehouses_WarehouseId",
                table: "Shipments",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Warehouses_WarehouseId",
                table: "Shipments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DriverLicense",
                table: "DriverLicense");

            migrationBuilder.DropIndex(
                name: "IX_DriverLicense_LicenseNumber",
                table: "DriverLicense");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Drivers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DriverLicense",
                table: "DriverLicense",
                column: "LicenseNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DriverLicense_DriverId",
                table: "DriverLicense",
                column: "DriverId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_AspNetUsers_UserId",
                table: "Drivers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Warehouses_WarehouseId",
                table: "Shipments",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
