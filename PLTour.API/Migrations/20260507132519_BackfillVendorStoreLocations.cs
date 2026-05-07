using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PLTour.API.Migrations
{
    /// <inheritdoc />
    public partial class BackfillVendorStoreLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""Locations"" (""Name"", ""Description"", ""Latitude"", ""Longitude"", ""Address"", ""CategoryId"", ""ImageUrl"", ""OrderIndex"", ""Radius"", ""IsActive"", ""CreatedDate"", ""UpdatedDate"")
                SELECT COALESCE(vs.""StoreName"", 'Cửa hàng'), COALESCE(vs.""Description"", ''), COALESCE(vs.""Latitude"", 0), COALESCE(vs.""Longitude"", 0), COALESCE(vs.""Address"", ''), 2, vs.""LogoUrl"", 0, 50, vs.""IsActive"", NOW(), NOW()
                FROM ""VendorStores"" vs
                WHERE vs.""LocationId"" IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""VendorStores"" vs
                SET ""LocationId"" = l.""LocationId""
                FROM ""Locations"" l
                WHERE vs.""LocationId"" IS NULL
                  AND l.""CategoryId"" = 2
                  AND l.""Name"" = COALESCE(vs.""StoreName"", 'Cửa hàng')
                  AND l.""Address"" = COALESCE(vs.""Address"", '')
                  AND l.""Latitude"" = COALESCE(vs.""Latitude"", 0)
                  AND l.""Longitude"" = COALESCE(vs.""Longitude"", 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
