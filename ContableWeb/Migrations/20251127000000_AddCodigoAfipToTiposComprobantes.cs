using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContableWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoAfipToTiposComprobantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Eliminar la columna Descripcion si existe
            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "AppTiposComprobantes");

            // Agregar columna CodigoAfip
            migrationBuilder.AddColumn<int>(
                name: "CodigoAfip",
                table: "AppTiposComprobantes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Agregar columnas de fechas si no existen
            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaDesde",
                table: "AppTiposComprobantes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaHasta",
                table: "AppTiposComprobantes",
                type: "date",
                nullable: true);

            // Crear índice único para CodigoAfip
            migrationBuilder.CreateIndex(
                name: "IX_AppTiposComprobantes_CodigoAfip",
                table: "AppTiposComprobantes",
                column: "CodigoAfip",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminar índice
            migrationBuilder.DropIndex(
                name: "IX_AppTiposComprobantes_CodigoAfip",
                table: "AppTiposComprobantes");

            // Eliminar columnas
            migrationBuilder.DropColumn(
                name: "CodigoAfip",
                table: "AppTiposComprobantes");

            migrationBuilder.DropColumn(
                name: "FechaDesde",
                table: "AppTiposComprobantes");

            migrationBuilder.DropColumn(
                name: "FechaHasta",
                table: "AppTiposComprobantes");

            // Restaurar columna Descripcion
            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "AppTiposComprobantes",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

