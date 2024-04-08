using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monkify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedExtraParameter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptDuplicatedCharacters",
                table: "SessionParameters",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptDuplicatedCharacters",
                table: "SessionParameters");
        }
    }
}
