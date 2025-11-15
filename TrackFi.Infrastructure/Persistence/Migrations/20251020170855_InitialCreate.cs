using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackFi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    primary_wallet_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    primary_wallet_network = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cover_picture_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cover_nft_contract = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cover_nft_token_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cover_nft_network = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_active_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    network = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    signature_proof = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    signature_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_wallets", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_wallets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "watchlist",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    network = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watchlist", x => x.id);
                    table.ForeignKey(
                        name: "FK_watchlist_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_wallets_address",
                table: "user_wallets",
                column: "wallet_address");

            migrationBuilder.CreateIndex(
                name: "idx_user_wallets_unique",
                table: "user_wallets",
                columns: new[] { "user_id", "wallet_address", "network" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_wallets_user",
                table: "user_wallets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_created_at",
                table: "users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_users_wallet",
                table: "users",
                column: "primary_wallet_address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_watchlist_added_at",
                table: "watchlist",
                column: "added_at");

            migrationBuilder.CreateIndex(
                name: "idx_watchlist_address",
                table: "watchlist",
                column: "wallet_address");

            migrationBuilder.CreateIndex(
                name: "idx_watchlist_unique",
                table: "watchlist",
                columns: new[] { "user_id", "wallet_address", "network" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_watchlist_user",
                table: "watchlist",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_wallets");

            migrationBuilder.DropTable(
                name: "watchlist");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
