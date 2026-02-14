using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationalMessenger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedFieldsToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Messages",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 13, 15, 45, 24, 664, DateTimeKind.Local).AddTicks(1041), "$2a$11$R7gC3VDjPOrYFBE8IgjXXu/ssUNR3u/7uNYuqRPn8OVZ1VeSgjLDq" });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 15, 45, 24, 695, DateTimeKind.Local).AddTicks(7031));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 15, 45, 24, 695, DateTimeKind.Local).AddTicks(7045));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 15, 45, 24, 695, DateTimeKind.Local).AddTicks(7047));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 15, 45, 24, 695, DateTimeKind.Local).AddTicks(7048));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 15, 45, 24, 695, DateTimeKind.Local).AddTicks(7049));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 15, 45, 24, 695, DateTimeKind.Local).AddTicks(7050));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 15, 45, 24, 695, DateTimeKind.Local).AddTicks(7051));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 15, 45, 24, 695, DateTimeKind.Local).AddTicks(7052));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 9,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 15, 45, 24, 695, DateTimeKind.Local).AddTicks(7053));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 10,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 15, 45, 24, 695, DateTimeKind.Local).AddTicks(7054));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 11,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 15, 45, 24, 695, DateTimeKind.Local).AddTicks(7055));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Messages");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 10, 14, 1, 27, 111, DateTimeKind.Local).AddTicks(3959), "$2a$11$YEsn4oADy3r3FV42dIufneNTPQ2hBYH3Ke9sfQl784pc8RD6j6pBe" });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 14, 1, 27, 151, DateTimeKind.Local).AddTicks(7222));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 14, 1, 27, 151, DateTimeKind.Local).AddTicks(7241));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 14, 1, 27, 151, DateTimeKind.Local).AddTicks(7242));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 14, 1, 27, 151, DateTimeKind.Local).AddTicks(7243));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 14, 1, 27, 151, DateTimeKind.Local).AddTicks(7244));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 14, 1, 27, 151, DateTimeKind.Local).AddTicks(7245));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 14, 1, 27, 151, DateTimeKind.Local).AddTicks(7247));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 14, 1, 27, 151, DateTimeKind.Local).AddTicks(7248));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 9,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 14, 1, 27, 151, DateTimeKind.Local).AddTicks(7249));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 10,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 14, 1, 27, 151, DateTimeKind.Local).AddTicks(7250));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 11,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 10, 14, 1, 27, 151, DateTimeKind.Local).AddTicks(7252));
        }
    }
}
