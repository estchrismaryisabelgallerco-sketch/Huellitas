using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Huellitas.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVeterinarioACitas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PersonalId",
                table: "Citas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Citas_PersonalId",
                table: "Citas",
                column: "PersonalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Citas_Personal_PersonalId",
                table: "Citas",
                column: "PersonalId",
                principalTable: "Personal",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Citas_Personal_PersonalId",
                table: "Citas");

            migrationBuilder.DropIndex(
                name: "IX_Citas_PersonalId",
                table: "Citas");

            migrationBuilder.DropColumn(
                name: "PersonalId",
                table: "Citas");
        }
    }
}
