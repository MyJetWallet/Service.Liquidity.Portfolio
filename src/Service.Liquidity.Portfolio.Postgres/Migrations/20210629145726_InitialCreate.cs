using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Service.Liquidity.Portfolio.Postgres.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "liquidityportfolio");

            migrationBuilder.CreateTable(
                name: "assetbalance",
                schema: "liquidityportfolio",
                columns: table => new
                {
                    WalletId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Asset = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Volume = table.Column<double>(type: "double precision", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assetbalance", x => new { x.WalletId, x.Asset });
                });

            migrationBuilder.CreateTable(
                name: "trade",
                schema: "liquidityportfolio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TradeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    WalletId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    BaseVolume = table.Column<double>(type: "double precision", nullable: false),
                    QuoteVolume = table.Column<double>(type: "double precision", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trade", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assetbalance",
                schema: "liquidityportfolio");

            migrationBuilder.DropTable(
                name: "trade",
                schema: "liquidityportfolio");
        }
    }
}
