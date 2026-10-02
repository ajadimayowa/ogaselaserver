using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ogasela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEngagementAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListingDailyStats",
                columns: table => new
                {
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Impressions = table.Column<long>(type: "bigint", nullable: false),
                    Views = table.Column<long>(type: "bigint", nullable: false),
                    CallClicks = table.Column<long>(type: "bigint", nullable: false),
                    MessageStarts = table.Column<long>(type: "bigint", nullable: false),
                    Saves = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingDailyStats", x => new { x.ListingId, x.Date });
                });

            migrationBuilder.CreateTable(
                name: "SellerDailyStats",
                columns: table => new
                {
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ProfileVisits = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerDailyStats", x => new { x.SellerId, x.Date });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingDailyStats_SellerId_Date",
                table: "ListingDailyStats",
                columns: new[] { "SellerId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingDailyStats");

            migrationBuilder.DropTable(
                name: "SellerDailyStats");
        }
    }
}
