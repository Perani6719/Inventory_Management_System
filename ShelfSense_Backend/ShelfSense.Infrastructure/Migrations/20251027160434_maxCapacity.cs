using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelfSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class maxCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxCapacity",
                table: "ProductShelves",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxCapacity",
                table: "ProductShelves");
        }
    }
}
