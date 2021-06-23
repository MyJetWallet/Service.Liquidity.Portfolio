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
                name: "trade",
                schema: "liquidityportfolio",
                columns: table => new
                {
                    SequenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WalletId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InstrumentSymbol = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Price = table.Column<double>(type: "double precision", nullable: false),
                    BaseVolume = table.Column<double>(type: "double precision", nullable: false),
                    QuoteVolume = table.Column<double>(type: "double precision", nullable: false),
                    OrderId = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Type = table.Column<int>(type: "integer", maxLength: 64, nullable: false),
                    OrderVolume = table.Column<double>(type: "double precision", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp without time zone", maxLength: 16, nullable: false),
                    TradeUId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Side = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trade", x => x.SequenceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trade_SequenceId",
                schema: "liquidityportfolio",
                table: "trade",
                column: "SequenceId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trade",
                schema: "liquidityportfolio");
        }
    }
}
