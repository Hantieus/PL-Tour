using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PLTour.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTourTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 5, 28, 31, 404, DateTimeKind.Utc).AddTicks(8280));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 5, 28, 31, 404, DateTimeKind.Utc).AddTicks(8284));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 5, 28, 31, 404, DateTimeKind.Utc).AddTicks(8286));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 5, 28, 31, 404, DateTimeKind.Utc).AddTicks(8287));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 19, 5, 28, 31, 404, DateTimeKind.Utc).AddTicks(8288));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 4, 19, 5, 28, 31, 510, DateTimeKind.Utc).AddTicks(3218), "$2a$11$IvsCNAERV8Hg1RhAyYZOOeIPPxZBW19vZ.7h.mLPiPKVL280xyPOC" });
        }
    }
}
