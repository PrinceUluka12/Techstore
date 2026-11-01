using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStore.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsletterStockAlertsOrderClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OrdersClaimTokenExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrdersClaimTokenHash",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobRuns",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastRunOn = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRuns", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "NewsletterSubscribers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UnsubscribeToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubscribedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnsubscribedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSubscribers", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "JobRuns",
                columns: new[] { "Name", "LastRunOn" },
                values: new object[] { "LowStockAlert", new DateOnly(2026, 1, 1) });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_Email",
                table: "NewsletterSubscribers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_UnsubscribeToken",
                table: "NewsletterSubscribers",
                column: "UnsubscribeToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobRuns");

            migrationBuilder.DropTable(
                name: "NewsletterSubscribers");

            migrationBuilder.DropColumn(
                name: "OrdersClaimTokenExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrdersClaimTokenHash",
                table: "Users");
        }
    }
}
