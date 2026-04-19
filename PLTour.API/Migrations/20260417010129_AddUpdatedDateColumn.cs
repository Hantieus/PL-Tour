using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PLTour.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedDateColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "IntroText",
                table: "Tours",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Tours",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<int>(
                name: "Duration",
                table: "Tours",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Tours",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 17, 8, 1, 26, 777, DateTimeKind.Local).AddTicks(8865));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 17, 8, 1, 26, 777, DateTimeKind.Local).AddTicks(8885));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 17, 8, 1, 26, 777, DateTimeKind.Local).AddTicks(8888));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 17, 8, 1, 26, 777, DateTimeKind.Local).AddTicks(8891));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 17, 8, 1, 26, 777, DateTimeKind.Local).AddTicks(8893));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 4, 17, 8, 1, 27, 30, DateTimeKind.Local).AddTicks(7280), "$2a$11$8loAOLRr2Rq5GYKyixKbj.ANCfnw299CKnia5I0zx.x4.ZANpSzfS" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Tours");

            migrationBuilder.AlterColumn<string>(
                name: "IntroText",
                table: "Tours",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Tours",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Duration",
                table: "Tours",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 9, 20, 21, 19, 481, DateTimeKind.Local).AddTicks(6340));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 9, 20, 21, 19, 481, DateTimeKind.Local).AddTicks(6358));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 9, 20, 21, 19, 481, DateTimeKind.Local).AddTicks(6360));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 9, 20, 21, 19, 481, DateTimeKind.Local).AddTicks(6362));

            migrationBuilder.UpdateData(
                table: "Languages",
                keyColumn: "LanguageId",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2026, 4, 9, 20, 21, 19, 481, DateTimeKind.Local).AddTicks(6363));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "PasswordHash" },
                values: new object[] { new DateTime(2026, 4, 9, 20, 21, 19, 655, DateTimeKind.Local).AddTicks(3758), "$2a$11$2n0GpUaurhEYJExisaQ76.vWz4k32TUAGOdHxRe24iTm4ZMWBtt8S" });
        }
    }
}
