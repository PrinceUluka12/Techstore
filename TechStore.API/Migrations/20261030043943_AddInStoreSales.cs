using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStore.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInStoreSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalPinHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedPinAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PinLockoutUntil",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Online"); // every existing order came from the website

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountApprovedByUserId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountReason",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoldByUserId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresImei",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "OrderItemSerials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemSerials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemSerials_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tendered = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPayments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderPayments_Users_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Permissions",
                value: "[\"orders.view\",\"orders.manage\",\"products.view\",\"products.manage\",\"inventory.manage\",\"users.view\",\"users.manage\",\"roles.manage\",\"reports.view\",\"coupons.manage\",\"images.manage\",\"stores.manage\",\"pos.sell\",\"pos.discount.approve\"]");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "RequiresImei" },
                values: new object[] { new DateTime(2026, 10, 30, 4, 39, 42, 549, DateTimeKind.Utc).AddTicks(7445), true });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "RequiresImei" },
                values: new object[] { new DateTime(2026, 10, 30, 4, 39, 42, 549, DateTimeKind.Utc).AddTicks(7448), false });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "RequiresImei" },
                values: new object[] { new DateTime(2026, 10, 30, 4, 39, 42, 549, DateTimeKind.Utc).AddTicks(7451), false });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "RequiresImei" },
                values: new object[] { new DateTime(2026, 10, 30, 4, 39, 42, 549, DateTimeKind.Utc).AddTicks(7454), false });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "RequiresImei" },
                values: new object[] { new DateTime(2026, 10, 30, 4, 39, 42, 549, DateTimeKind.Utc).AddTicks(7457), false });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Channel_StoreId_CreatedAt",
                table: "Orders",
                columns: new[] { "Channel", "StoreId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DiscountApprovedByUserId",
                table: "Orders",
                column: "DiscountApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SoldByUserId",
                table: "Orders",
                column: "SoldByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemSerials_OrderItemId",
                table: "OrderItemSerials",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemSerials_SerialNumber",
                table: "OrderItemSerials",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_OrderId",
                table: "OrderPayments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_ReceivedByUserId",
                table: "OrderPayments",
                column: "ReceivedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_DiscountApprovedByUserId",
                table: "Orders",
                column: "DiscountApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_SoldByUserId",
                table: "Orders",
                column: "SoldByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Orders need an account again, so walk-in sales without one can't be kept
            migrationBuilder.Sql("DELETE FROM Orders WHERE UserId IS NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_DiscountApprovedByUserId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_SoldByUserId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OrderItemSerials");

            migrationBuilder.DropTable(
                name: "OrderPayments");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Channel_StoreId_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DiscountApprovedByUserId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SoldByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ApprovalPinHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FailedPinAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PinLockoutUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountApprovedByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SoldByUserId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "RequiresImei",
                table: "Categories");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AppRoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Permissions",
                value: "[\"orders.view\",\"orders.manage\",\"products.view\",\"products.manage\",\"inventory.manage\",\"users.view\",\"users.manage\",\"roles.manage\",\"reports.view\",\"coupons.manage\",\"images.manage\",\"stores.manage\"]");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 3, 30, 54, 157, DateTimeKind.Utc).AddTicks(7304));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 3, 30, 54, 157, DateTimeKind.Utc).AddTicks(7309));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 3, 30, 54, 157, DateTimeKind.Utc).AddTicks(7313));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 3, 30, 54, 157, DateTimeKind.Utc).AddTicks(7317));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 30, 3, 30, 54, 157, DateTimeKind.Utc).AddTicks(7321));
        }
    }
}
