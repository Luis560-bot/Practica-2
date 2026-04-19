using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Practica2.Migrations
{
    /// <inheritdoc />
    public partial class AjustarRestriccionesCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Curso_Creditos",
                table: "Cursos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Curso_Horario",
                table: "Cursos");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Curso_Creditos",
                table: "Cursos",
                sql: "\"Creditos\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Curso_Horario",
                table: "Cursos",
                sql: "\"HorarioInicio\" <= \"HorarioFin\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Curso_Creditos",
                table: "Cursos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Curso_Horario",
                table: "Cursos");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Curso_Creditos",
                table: "Cursos",
                sql: "\"Creditos\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Curso_Horario",
                table: "Cursos",
                sql: "\"HorarioInicio\" < \"HorarioFin\"");
        }
    }
}
