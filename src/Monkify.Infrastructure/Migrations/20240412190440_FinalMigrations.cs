using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monkify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinalMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionBets_Users_UserId",
                table: "SessionBets");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SessionBets_UserId",
                table: "SessionBets");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SessionBets");

            migrationBuilder.DropColumn(
                name: "Wallet",
                table: "BetLogs");

            migrationBuilder.AddColumn<string>(
                name: "Wallet",
                table: "SessionBets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Wallet",
                table: "SessionBets");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "SessionBets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Wallet",
                table: "BetLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Wallet = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionBets_UserId",
                table: "SessionBets",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionBets_Users_UserId",
                table: "SessionBets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
