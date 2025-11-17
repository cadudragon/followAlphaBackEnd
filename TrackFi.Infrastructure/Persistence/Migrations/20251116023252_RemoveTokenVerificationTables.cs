using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackFi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTokenVerificationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenMetadata");

            migrationBuilder.DropTable(
                name: "UnlistedTokens");

            migrationBuilder.DropTable(
                name: "VerifiedTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractAddress = table.Column<string>(type: "text", nullable: false),
                    Decimals = table.Column<int>(type: "integer", nullable: false),
                    EncounterCount = table.Column<int>(type: "integer", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Network = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnlistedTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckCount = table.Column<int>(type: "integer", nullable: false),
                    ContractAddress = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Network = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnlistedTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerifiedTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoinGeckoId = table.Column<string>(type: "text", nullable: true),
                    ContractAddress = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Decimals = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsNative = table.Column<bool>(type: "boolean", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Network = table.Column<string>(type: "text", nullable: false),
                    Standard = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedBy = table.Column<string>(type: "text", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiedTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenMetadata_ContractAddress_Network",
                table: "TokenMetadata",
                columns: new[] { "ContractAddress", "Network" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenMetadata_EncounterCount",
                table: "TokenMetadata",
                column: "EncounterCount");

            migrationBuilder.CreateIndex(
                name: "IX_TokenMetadata_Symbol",
                table: "TokenMetadata",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_UnlistedTokens_ContractAddress_Network",
                table: "UnlistedTokens",
                columns: new[] { "ContractAddress", "Network" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnlistedTokens_LastCheckedAt",
                table: "UnlistedTokens",
                column: "LastCheckedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedTokens_ContractAddress_Network",
                table: "VerifiedTokens",
                columns: new[] { "ContractAddress", "Network" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedTokens_Status",
                table: "VerifiedTokens",
                column: "Status");
        }
    }
}
