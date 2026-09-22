using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ogasela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdAccountConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "text", nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "text", nullable: true),
                    ExternalAccountId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ConnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdAccountConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalCampaignId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BudgetKobo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastMetricsSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MetricsSnapshotJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdCampaigns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdAccountConnections_SellerId_Platform",
                table: "AdAccountConnections",
                columns: new[] { "SellerId", "Platform" });

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_ListingId_Platform_CreatedAt",
                table: "AdCampaigns",
                columns: new[] { "ListingId", "Platform", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_Status",
                table: "AdCampaigns",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdAccountConnections");

            migrationBuilder.DropTable(
                name: "AdCampaigns");
        }
    }
}
