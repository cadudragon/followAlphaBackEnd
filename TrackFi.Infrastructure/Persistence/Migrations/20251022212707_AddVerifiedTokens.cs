using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackFi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifiedTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerifiedTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractAddress = table.Column<string>(type: "text", nullable: false),
                    Network = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Decimals = table.Column<int>(type: "integer", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    CoinGeckoId = table.Column<string>(type: "text", nullable: true),
                    Standard = table.Column<string>(type: "text", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    VerifiedBy = table.Column<string>(type: "text", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsNative = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiedTokens", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerifiedTokens");
        }
    }
}
