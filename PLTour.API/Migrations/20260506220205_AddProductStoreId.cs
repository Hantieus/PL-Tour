using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PLTour.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProductStoreId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS ""Products"" ADD COLUMN IF NOT EXISTS ""StoreId"" integer;");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Products_StoreId"" ON ""Products"" (""StoreId"");");
            migrationBuilder.Sql(@"DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Products_VendorStores_StoreId'
    ) THEN
        ALTER TABLE ""Products""
        ADD CONSTRAINT ""FK_Products_VendorStores_StoreId""
        FOREIGN KEY (""StoreId"") REFERENCES ""VendorStores"" (""StoreId"") ON DELETE SET NULL;
    END IF;
END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS ""Products"" DROP CONSTRAINT IF EXISTS ""FK_Products_VendorStores_StoreId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_StoreId"";");
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS ""Products"" DROP COLUMN IF EXISTS ""StoreId"";");
        }
    }
}
