using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PLTour.API.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorStoreLocationLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "VendorStores",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorStores_LocationId",
                table: "VendorStores",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorStores_Locations_LocationId",
                table: "VendorStores",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "LocationId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorStores_Locations_LocationId",
                table: "VendorStores");

            migrationBuilder.DropIndex(
                name: "IX_VendorStores_LocationId",
                table: "VendorStores");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "VendorStores");
        }
    }
}
