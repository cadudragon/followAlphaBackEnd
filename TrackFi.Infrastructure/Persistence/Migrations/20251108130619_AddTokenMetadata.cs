using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackFi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractAddress = table.Column<string>(type: "text", nullable: false),
                    Network = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Decimals = table.Column<int>(type: "integer", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EncounterCount = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenMetadata", x => x.Id);
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenMetadata");
        }
    }
}
