using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Service.Liquidity.Portfolio.Postgres.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "liquidityportfolio");

            migrationBuilder.CreateTable(
                name: "trade",
                schema: "liquidityportfolio",
                columns: table => new
                {
                    TradeUId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WalletId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InstrumentSymbol = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    BaseVolume = table.Column<double>(type: "double precision", nullable: false),
                    QuoteVolume = table.Column<double>(type: "double precision", nullable: false),
                    OrderId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Type = table.Column<int>(type: "integer", maxLength: 64, nullable: false),
                    OrderVolume = table.Column<double>(type: "double precision", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    SequenceId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trade", x => x.TradeUId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trade",
                schema: "liquidityportfolio");
        }
    }
}
