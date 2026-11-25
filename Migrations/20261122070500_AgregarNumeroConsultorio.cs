using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Huellitas.Migrations
{
    /// <inheritdoc />
    public partial class AgregarNumeroConsultorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumeroConsultorio",
                table: "HorariosPersonal",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE HorariosPersonal
                SET NumeroConsultorio = 1
                WHERE Id = 1;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "NumeroConsultorio",
                table: "HorariosPersonal",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroConsultorio",
                table: "HorariosPersonal");
        }
    }
}
