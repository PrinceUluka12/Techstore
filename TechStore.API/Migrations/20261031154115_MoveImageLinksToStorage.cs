using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechStore.API.Migrations
{
    /// <inheritdoc />
    public partial class MoveImageLinksToStorage : Migration
    {
        // Images were served through Azure Front Door, which is being removed to save its monthly
        // base fee. The same files are in the storage account at the same paths, so saved links
        // only need their host changed. Links anywhere else (or already moved) are left alone.
        private const string FrontDoor = "https://hytel-deheb9e5gteqb7cr.z01.azurefd.net/";
        private const string Storage   = "https://hytelstorage.blob.core.windows.net/";

        private static void MoveLinks(MigrationBuilder migrationBuilder, string from, string to)
        {
            migrationBuilder.Sql($"UPDATE Products SET ImageUrl = REPLACE(ImageUrl, '{from}', '{to}') WHERE ImageUrl LIKE '{from}%';");
            migrationBuilder.Sql($"UPDATE Products SET AdditionalImages = REPLACE(AdditionalImages, '{from}', '{to}') WHERE AdditionalImages LIKE '%{from}%';");
            migrationBuilder.Sql($"UPDATE Categories SET ImageUrl = REPLACE(ImageUrl, '{from}', '{to}') WHERE ImageUrl LIKE '{from}%';");
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MoveLinks(migrationBuilder, FrontDoor, Storage);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 31, 15, 41, 14, 702, DateTimeKind.Utc).AddTicks(1530));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 31, 15, 41, 14, 702, DateTimeKind.Utc).AddTicks(1532));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 31, 15, 41, 14, 702, DateTimeKind.Utc).AddTicks(1534));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 31, 15, 41, 14, 702, DateTimeKind.Utc).AddTicks(1535));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 31, 15, 41, 14, 702, DateTimeKind.Utc).AddTicks(1537));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MoveLinks(migrationBuilder, Storage, FrontDoor);

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
    }
}
