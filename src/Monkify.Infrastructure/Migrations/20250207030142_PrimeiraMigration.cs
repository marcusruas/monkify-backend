using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monkify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PrimeiraMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SessionCharacterType",
                table: "SessionParameters",
                newName: "AllowedCharacters");

            migrationBuilder.AddColumn<bool>(
                name: "PlayersDefineCharacters",
                table: "SessionParameters",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayersDefineCharacters",
                table: "SessionParameters");

            migrationBuilder.RenameColumn(
                name: "AllowedCharacters",
                table: "SessionParameters",
                newName: "SessionCharacterType");
        }
    }
}
