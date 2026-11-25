using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Huellitas.Migrations
{
    /// <inheritdoc />
    public partial class EliminarNumeroColegiatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroColegiatura",
                table: "Personal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumeroColegiatura",
                table: "Personal",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
