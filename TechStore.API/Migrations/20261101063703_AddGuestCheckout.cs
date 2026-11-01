using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStore.API.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestToken",
                table: "Orders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_GuestToken",
                table: "Orders",
                column: "GuestToken",
                unique: true,
                filter: "[GuestToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_GuestToken",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GuestToken",
                table: "Orders");
        }
    }
}
