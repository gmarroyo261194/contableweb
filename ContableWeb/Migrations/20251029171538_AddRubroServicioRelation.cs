using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContableWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddRubroServicioRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppServicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RubroId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RubroId1 = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppServicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppServicios_AppRubros_RubroId",
                        column: x => x.RubroId,
                        principalTable: "AppRubros",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppServicios_AppRubros_RubroId1",
                        column: x => x.RubroId1,
                        principalTable: "AppRubros",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppServicios_Nombre",
                table: "AppServicios",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppServicios_RubroId",
                table: "AppServicios",
                column: "RubroId");

            migrationBuilder.CreateIndex(
                name: "IX_AppServicios_RubroId1",
                table: "AppServicios",
                column: "RubroId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppServicios");
        }
    }
}
