using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monkify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedExtraParameter5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "RequiredAmount",
                table: "SessionParameters",
                type: "float(9)",
                precision: 9,
                scale: 8,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(8)",
                oldPrecision: 8,
                oldScale: 8);

            migrationBuilder.AlterColumn<double>(
                name: "Amount",
                table: "SessionBets",
                type: "float(9)",
                precision: 9,
                scale: 8,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(8)",
                oldPrecision: 8,
                oldScale: 8);

            migrationBuilder.AlterColumn<double>(
                name: "Amount",
                table: "BetLogs",
                type: "float(9)",
                precision: 9,
                scale: 8,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(8)",
                oldPrecision: 8,
                oldScale: 8);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "RequiredAmount",
                table: "SessionParameters",
                type: "float(8)",
                precision: 8,
                scale: 8,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(9)",
                oldPrecision: 9,
                oldScale: 8);

            migrationBuilder.AlterColumn<double>(
                name: "Amount",
                table: "SessionBets",
                type: "float(8)",
                precision: 8,
                scale: 8,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(9)",
                oldPrecision: 9,
                oldScale: 8);

            migrationBuilder.AlterColumn<double>(
                name: "Amount",
                table: "BetLogs",
                type: "float(8)",
                precision: 8,
                scale: 8,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(9)",
                oldPrecision: 9,
                oldScale: 8);
        }
    }
}
