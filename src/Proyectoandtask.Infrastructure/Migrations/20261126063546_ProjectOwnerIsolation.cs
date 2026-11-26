using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyectoandtask.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProjectOwnerIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Proyectos_Name",
                table: "Proyectos");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Proyectos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_Name",
                table: "Proyectos",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Proyectos_AspNetUsers_OwnerId",
                table: "Proyectos",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proyectos_AspNetUsers_OwnerId",
                table: "Proyectos");

            migrationBuilder.DropIndex(
                name: "IX_Proyectos_Name",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Proyectos");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_Name",
                table: "Proyectos",
                column: "Name",
                unique: true);
        }
    }
}
