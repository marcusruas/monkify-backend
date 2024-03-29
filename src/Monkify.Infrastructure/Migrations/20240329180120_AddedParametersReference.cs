using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monkify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedParametersReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CharacterType",
                table: "Sessions");

            migrationBuilder.AddColumn<Guid>(
                name: "ParametersId",
                table: "Sessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ParametersId",
                table: "Sessions",
                column: "ParametersId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_SessionParameters_ParametersId",
                table: "Sessions",
                column: "ParametersId",
                principalTable: "SessionParameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_SessionParameters_ParametersId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_ParametersId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ParametersId",
                table: "Sessions");

            migrationBuilder.AddColumn<int>(
                name: "CharacterType",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
