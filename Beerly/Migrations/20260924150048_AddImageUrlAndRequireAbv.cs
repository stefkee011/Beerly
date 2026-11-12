using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beerly.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlAndRequireAbv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "AbvPercentage",
                table: "Beers",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Beers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Beers");

            migrationBuilder.AlterColumn<double>(
                name: "AbvPercentage",
                table: "Beers",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
