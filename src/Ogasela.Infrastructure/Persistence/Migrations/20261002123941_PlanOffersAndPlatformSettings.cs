using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ogasela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanOffersAndPlatformSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PromotionPlans",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PromotionPlans",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Features",
                table: "PromotionPlans",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PlatformSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSettings", x => x.Key);
                });

            migrationBuilder.UpdateData(
                table: "PromotionPlans",
                keyColumn: "Id",
                keyValue: new Guid("91a70000-0000-0000-0000-000000000001"),
                columns: new[] { "Description", "Features" },
                values: new object[] { "Get your ad in front of buyers at no cost.", "Live for 7 days\nUp to 5 photos\nAI listing helper (basic)" });

            migrationBuilder.UpdateData(
                table: "PromotionPlans",
                keyColumn: "Id",
                keyValue: new Guid("91a70000-0000-0000-0000-000000000002"),
                columns: new[] { "Description", "Features" },
                values: new object[] { "A longer run and a small boost in search.", "Live for 15 days\nUp to 8 photos\nSmall boost in search results\nPromote on TikTok and Facebook\nAI listing helper (standard)" });

            migrationBuilder.UpdateData(
                table: "PromotionPlans",
                keyColumn: "Id",
                keyValue: new Guid("91a70000-0000-0000-0000-000000000003"),
                columns: new[] { "Description", "Features" },
                values: new object[] { "More photos, video and a stronger boost.", "Live for 21 days\nUp to 10 photos and a video\nStronger boost in search results\nPromote on TikTok and Facebook\nFull AI listing tools" });

            migrationBuilder.UpdateData(
                table: "PromotionPlans",
                keyColumn: "Id",
                keyValue: new Guid("91a70000-0000-0000-0000-000000000004"),
                columns: new[] { "Description", "Features" },
                values: new object[] { "Maximum visibility, with ad credit included.", "Live for 30 days\nUp to 12 photos and a video\nTop boost in search results\nPromote on TikTok and Facebook\n₦2,000 ad credit included\nFull AI listing tools" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PromotionPlans");

            migrationBuilder.DropColumn(
                name: "Features",
                table: "PromotionPlans");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PromotionPlans",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);
        }
    }
}
