using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monkify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedTxLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WalletId",
                table: "Users",
                newName: "Wallet");

            migrationBuilder.RenameColumn(
                name: "BetChoice",
                table: "SessionBets",
                newName: "Choice");

            migrationBuilder.RenameColumn(
                name: "BetAmount",
                table: "SessionBets",
                newName: "Amount");

            migrationBuilder.CreateTable(
                name: "BetLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<double>(type: "float(8)", precision: 8, scale: 8, nullable: false),
                    Wallet = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Signature = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BetLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BetLogs_SessionBets_BetId",
                        column: x => x.BetId,
                        principalTable: "SessionBets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BetLogs_BetId",
                table: "BetLogs",
                column: "BetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BetLogs");

            migrationBuilder.RenameColumn(
                name: "Wallet",
                table: "Users",
                newName: "WalletId");

            migrationBuilder.RenameColumn(
                name: "Choice",
                table: "SessionBets",
                newName: "BetChoice");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "SessionBets",
                newName: "BetAmount");
        }
    }
}
