using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monkify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedParametersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionBets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BetAmount = table.Column<decimal>(type: "decimal(8,8)", precision: 8, scale: 8, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionBets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionBets_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionBets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionCharacterType = table.Column<int>(type: "int", nullable: false),
                    MinimumNumberOfPlayers = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Active = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionParameters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionBets_SessionId",
                table: "SessionBets",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionBets_UserId",
                table: "SessionBets",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionBets");

            migrationBuilder.DropTable(
                name: "SessionParameters");
        }
    }
}
