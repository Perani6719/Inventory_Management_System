using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShelfSense.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class removesimages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Category",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
