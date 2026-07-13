using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Beerly.Migrations
{
    /// <inheritdoc />
    public partial class FixAbvAndCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AbvPrecentage",
                table: "Beers",
                newName: "AbvPercentage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AbvPercentage",
                table: "Beers",
                newName: "AbvPrecentage");
        }
    }
}
