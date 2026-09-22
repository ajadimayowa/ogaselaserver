using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ogasela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBiometricVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BiometricConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricConsents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BiometricVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    RekognitionFaceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LivenessScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    IdMatchScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DecisionSource = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    SelfieImageS3Key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IdPhotoImageS3Key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RawImageExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsErased = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerificationAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BiometricVerificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BiometricConsents_SellerId",
                table: "BiometricConsents",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricVerifications_RawImageExpiryDate",
                table: "BiometricVerifications",
                column: "RawImageExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricVerifications_SellerId",
                table: "BiometricVerifications",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationAuditEntries_BiometricVerificationId",
                table: "VerificationAuditEntries",
                column: "BiometricVerificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BiometricConsents");

            migrationBuilder.DropTable(
                name: "BiometricVerifications");

            migrationBuilder.DropTable(
                name: "VerificationAuditEntries");
        }
    }
}
