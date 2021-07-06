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
                    WalletName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Asset = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BrokerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Volume = table.Column<double>(type: "double precision", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assetbalance", x => new { x.WalletName, x.Asset });
                });

            migrationBuilder.CreateTable(
                name: "changebalancehistory",
                schema: "liquidityportfolio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrokerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    WalletName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Asset = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    VolumeDifference = table.Column<double>(type: "double precision", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Comment = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    User = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_changebalancehistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trade",
                schema: "liquidityportfolio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TradeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BrokerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    WalletName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Symbol = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    BaseVolume = table.Column<double>(type: "double precision", nullable: false),
                    QuoteVolume = table.Column<double>(type: "double precision", nullable: false),
                    BaseVolumeInUsd = table.Column<double>(type: "double precision", nullable: false),
                    QuoteVolumeInUsd = table.Column<double>(type: "double precision", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Comment = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    User = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trade", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trade_Source",
                schema: "liquidityportfolio",
                table: "trade",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_trade_TradeId",
                schema: "liquidityportfolio",
                table: "trade",
                column: "TradeId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assetbalance",
                schema: "liquidityportfolio");

            migrationBuilder.DropTable(
                name: "changebalancehistory",
                schema: "liquidityportfolio");

            migrationBuilder.DropTable(
                name: "trade",
                schema: "liquidityportfolio");
        }
    }
}
