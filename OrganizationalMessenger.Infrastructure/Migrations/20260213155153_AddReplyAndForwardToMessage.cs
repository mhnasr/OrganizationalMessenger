using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationalMessenger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReplyAndForwardToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ForwardedFromUserId",
                table: "Messages",
                type: "int",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ForwardedFromUserId",
                table: "Messages",
                column: "ForwardedFromUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_ForwardedFromUserId",
                table: "Messages",
                column: "ForwardedFromUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_ForwardedFromUserId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ForwardedFromUserId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ForwardedFromUserId",
                table: "Messages");

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
    }
}
