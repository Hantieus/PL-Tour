using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PLTour.Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddRadiusToLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Narrations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "Radius",
                table: "Locations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 3, 8, 27, 46, 183, DateTimeKind.Local).AddTicks(3229));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 3, 8, 27, 46, 183, DateTimeKind.Local).AddTicks(3246));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 3, 8, 27, 46, 183, DateTimeKind.Local).AddTicks(3247));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 3, 8, 27, 46, 183, DateTimeKind.Local).AddTicks(3248));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 3, 8, 27, 46, 183, DateTimeKind.Local).AddTicks(3249));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 4, 3, 8, 27, 46, 286, DateTimeKind.Local).AddTicks(7740), "$2a$11$SaJVriTtJPHnLMAJ6hF.v.yeoMHH1YYOI6sqHAiZai/wjVwBJg3O2" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Radius",
                table: "Locations");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Narrations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 20, 1, 41, 27, 284, DateTimeKind.Local).AddTicks(5661));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 20, 1, 41, 27, 284, DateTimeKind.Local).AddTicks(5680));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 20, 1, 41, 27, 284, DateTimeKind.Local).AddTicks(5682));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 20, 1, 41, 27, 284, DateTimeKind.Local).AddTicks(5683));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 3, 20, 1, 41, 27, 284, DateTimeKind.Local).AddTicks(5686));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 3, 20, 1, 41, 27, 389, DateTimeKind.Local).AddTicks(8646), "$2a$11$5fKNOKWZyLIhXxKe0AL.T.f5Y2BpiBriVlWaXr6.qvA9UGKAxjg5u" });
        }
    }
}
