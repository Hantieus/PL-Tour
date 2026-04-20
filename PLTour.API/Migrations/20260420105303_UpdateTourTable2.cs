using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PLTour.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTourTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 20, 10, 53, 2, 989, DateTimeKind.Utc).AddTicks(1250));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 20, 10, 53, 2, 989, DateTimeKind.Utc).AddTicks(1254));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 20, 10, 53, 2, 989, DateTimeKind.Utc).AddTicks(1256));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 20, 10, 53, 2, 989, DateTimeKind.Utc).AddTicks(1257));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 20, 10, 53, 2, 989, DateTimeKind.Utc).AddTicks(1259));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 4, 20, 10, 53, 3, 257, DateTimeKind.Utc).AddTicks(2820), "$2a$11$UlXxn0VECcMs3P4LkXTerOfLAQsZt3v.nTlEOE9KoC5QCg0kZAp4y" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 20, 8, 51, 58, 867, DateTimeKind.Utc).AddTicks(9920));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 20, 8, 51, 58, 867, DateTimeKind.Utc).AddTicks(9963));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 20, 8, 51, 58, 867, DateTimeKind.Utc).AddTicks(9964));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 20, 8, 51, 58, 867, DateTimeKind.Utc).AddTicks(9966));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 20, 8, 51, 58, 867, DateTimeKind.Utc).AddTicks(9968));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 4, 20, 8, 51, 59, 107, DateTimeKind.Utc).AddTicks(9024), "$2a$11$wQE/OaTgChpBmhhZ6UVy/ehlxH3yCHAreuIBd0wHrPfNQdctXxwfy" });
        }
    }
}
