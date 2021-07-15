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
                name: "Trades",
                schema: "liquidityportfolio",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TradeId = table.Column<string>(type: "text", nullable: true),
                    AssociateBrokerId = table.Column<string>(type: "text", nullable: true),
                    WalletName = table.Column<string>(type: "text", nullable: true),
                    AssociateSymbol = table.Column<string>(type: "text", nullable: true),
                    BaseAsset = table.Column<string>(type: "text", nullable: true),
                    QuoteAsset = table.Column<string>(type: "text", nullable: true),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    BaseVolume = table.Column<double>(type: "double precision", nullable: false),
                    QuoteVolume = table.Column<double>(type: "double precision", nullable: false),
                    BaseVolumeInUsd = table.Column<double>(type: "double precision", nullable: false),
                    QuoteVolumeInUsd = table.Column<double>(type: "double precision", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    User = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Id);
                });
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
                name: "Trades",
                schema: "liquidityportfolio");
        }
    }
}
