using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyectoandtask.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectStartDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaInicio",
                table: "Proyectos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Proyectos\" SET \"FechaInicio\" = \"FechaCreacion\" WHERE \"FechaInicio\" IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaInicio",
                table: "Proyectos",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaInicio",
                table: "Proyectos");
        }
    }
}
