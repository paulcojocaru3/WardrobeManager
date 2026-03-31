using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace WardrobeManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFashionClipFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "ClothingItems",
                type: "vector(512)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "ClothingItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Season",
                table: "ClothingItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Usage",
                table: "ClothingItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "ClothingItems");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "ClothingItems");

            migrationBuilder.DropColumn(
                name: "Season",
                table: "ClothingItems");

            migrationBuilder.DropColumn(
                name: "Usage",
                table: "ClothingItems");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
