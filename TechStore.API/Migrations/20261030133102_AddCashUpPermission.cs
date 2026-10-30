using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStore.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCashUpPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Permissions",
                value: "[\"orders.view\",\"orders.manage\",\"products.view\",\"products.manage\",\"inventory.manage\",\"users.view\",\"users.manage\",\"roles.manage\",\"reports.view\",\"coupons.manage\",\"images.manage\",\"stores.manage\",\"pos.sell\",\"pos.discount.approve\",\"pos.cashup.shop\",\"transfers.manage\"]");

            // Cashiers now see only their own sales in the cash-up. Roles that can approve
            // discounts (shop managers) keep the whole shop: add the permission to their
            // JSON list, before its closing ].
            migrationBuilder.Sql(
                "UPDATE AppRoles SET Permissions = STUFF(Permissions, LEN(Permissions), 1, ',\"pos.cashup.shop\"]') " +
                "WHERE Id <> 1 AND Permissions LIKE '%\"pos.discount.approve\"%' AND Permissions NOT LIKE '%\"pos.cashup.shop\"%';");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 13, 31, 1, 130, DateTimeKind.Utc).AddTicks(773));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 13, 31, 1, 130, DateTimeKind.Utc).AddTicks(776));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 13, 31, 1, 130, DateTimeKind.Utc).AddTicks(778));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 13, 31, 1, 130, DateTimeKind.Utc).AddTicks(780));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 13, 31, 1, 130, DateTimeKind.Utc).AddTicks(782));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The permission no longer exists, so take it out of every custom role
            migrationBuilder.Sql(
                "UPDATE AppRoles SET Permissions = REPLACE(REPLACE(REPLACE(Permissions, " +
                "',\"pos.cashup.shop\"', ''), '\"pos.cashup.shop\",', ''), '\"pos.cashup.shop\"', '') " +
                "WHERE Id <> 1 AND Permissions LIKE '%\"pos.cashup.shop\"%';");

            migrationBuilder.UpdateData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Permissions",
                value: "[\"orders.view\",\"orders.manage\",\"products.view\",\"products.manage\",\"inventory.manage\",\"users.view\",\"users.manage\",\"roles.manage\",\"reports.view\",\"coupons.manage\",\"images.manage\",\"stores.manage\",\"pos.sell\",\"pos.discount.approve\",\"transfers.manage\"]");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 12, 42, 28, 104, DateTimeKind.Utc).AddTicks(4071));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 12, 42, 28, 104, DateTimeKind.Utc).AddTicks(4077));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 12, 42, 28, 104, DateTimeKind.Utc).AddTicks(4080));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 12, 42, 28, 104, DateTimeKind.Utc).AddTicks(4083));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 12, 42, 28, 104, DateTimeKind.Utc).AddTicks(4086));
        }
    }
}
