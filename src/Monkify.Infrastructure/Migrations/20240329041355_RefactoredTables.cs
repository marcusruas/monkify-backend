using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monkify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactoredTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SessionCharacterType",
                table: "Sessions",
                newName: "CharacterType");

            migrationBuilder.AddColumn<string>(
                name: "BetChoice",
                table: "SessionBets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BetChoice",
                table: "SessionBets");

            migrationBuilder.RenameColumn(
                name: "CharacterType",
                table: "Sessions",
                newName: "SessionCharacterType");
        }
    }
}
