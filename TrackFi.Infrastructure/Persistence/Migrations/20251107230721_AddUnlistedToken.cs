using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackFi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnlistedToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnlistedTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractAddress = table.Column<string>(type: "text", nullable: false),
                    Network = table.Column<string>(type: "text", nullable: false),
                    Symbol = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    FirstCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnlistedTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnlistedTokens_ContractAddress_Network",
                table: "UnlistedTokens",
                columns: new[] { "ContractAddress", "Network" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnlistedTokens_LastCheckedAt",
                table: "UnlistedTokens",
                column: "LastCheckedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnlistedTokens");
        }
    }
}
