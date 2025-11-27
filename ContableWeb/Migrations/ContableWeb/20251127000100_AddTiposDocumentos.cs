#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace ContableWeb.Migrations.ContableWeb
{
    /// <inheritdoc />
    public partial class AddTiposDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTiposDocumentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoAfip = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaDesde = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaHasta = table.Column<DateOnly>(type: "date", nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTiposDocumentos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTiposDocumentos_CodigoAfip",
                table: "AppTiposDocumentos",
                column: "CodigoAfip",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTiposDocumentos");
        }
    }
}

