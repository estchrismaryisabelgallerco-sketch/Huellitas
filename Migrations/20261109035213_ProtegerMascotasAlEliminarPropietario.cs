using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Huellitas.Migrations
{
    /// <inheritdoc />
    public partial class ProtegerMascotasAlEliminarPropietario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mascotas_Propietarios_PropietarioId",
                table: "Mascotas");

            migrationBuilder.AddForeignKey(
                name: "FK_Mascotas_Propietarios_PropietarioId",
                table: "Mascotas",
                column: "PropietarioId",
                principalTable: "Propietarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mascotas_Propietarios_PropietarioId",
                table: "Mascotas");

            migrationBuilder.AddForeignKey(
                name: "FK_Mascotas_Propietarios_PropietarioId",
                table: "Mascotas",
                column: "PropietarioId",
                principalTable: "Propietarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
