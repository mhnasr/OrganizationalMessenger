using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationalMessenger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class reactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 15, 15, 9, 59, 640, DateTimeKind.Local).AddTicks(1401), "$2a$11$Y1S9gwwfHqCEwRFCGylc3.Tf762CAi3Lh4wdyaN5PwK4n9r12Z0ru" });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 15, 9, 59, 672, DateTimeKind.Local).AddTicks(8141));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 15, 9, 59, 672, DateTimeKind.Local).AddTicks(8157));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 15, 9, 59, 672, DateTimeKind.Local).AddTicks(8158));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 15, 9, 59, 672, DateTimeKind.Local).AddTicks(8159));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 15, 9, 59, 672, DateTimeKind.Local).AddTicks(8160));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 15, 9, 59, 672, DateTimeKind.Local).AddTicks(8161));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 15, 9, 59, 672, DateTimeKind.Local).AddTicks(8162));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 15, 9, 59, 672, DateTimeKind.Local).AddTicks(8163));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 9,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 15, 9, 59, 672, DateTimeKind.Local).AddTicks(8163));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 10,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 15, 9, 59, 672, DateTimeKind.Local).AddTicks(8164));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 11,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 15, 15, 9, 59, 672, DateTimeKind.Local).AddTicks(8165));

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_MessageId_UserId",
                table: "MessageReactions",
                columns: new[] { "MessageId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MessageReactions_MessageId_UserId",
                table: "MessageReactions");

            migrationBuilder.UpdateData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "PasswordHash" },
                values: new object[] { new DateTime(2026, 2, 13, 19, 21, 52, 697, DateTimeKind.Local).AddTicks(5634), "$2a$11$OsGP0yI.judOh0aBnGLYMOLgcEvOm6R5jhm.ir4oCV4L8Stts7RF6" });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 19, 21, 52, 748, DateTimeKind.Local).AddTicks(8131));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 19, 21, 52, 748, DateTimeKind.Local).AddTicks(8148));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 19, 21, 52, 748, DateTimeKind.Local).AddTicks(8150));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 19, 21, 52, 748, DateTimeKind.Local).AddTicks(8151));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 19, 21, 52, 748, DateTimeKind.Local).AddTicks(8152));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 6,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 19, 21, 52, 748, DateTimeKind.Local).AddTicks(8153));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 7,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 19, 21, 52, 748, DateTimeKind.Local).AddTicks(8155));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 8,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 19, 21, 52, 748, DateTimeKind.Local).AddTicks(8156));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 9,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 19, 21, 52, 748, DateTimeKind.Local).AddTicks(8157));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 10,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 19, 21, 52, 748, DateTimeKind.Local).AddTicks(8158));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 11,
                column: "UpdatedAt",
                value: new DateTime(2026, 2, 13, 19, 21, 52, 748, DateTimeKind.Local).AddTicks(8159));
        }
    }
}
