using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContableWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddTiposCondicionesIva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTiposCondicionesIva",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoAfip = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTiposCondicionesIva", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTiposCondicionesIva_CodigoAfip",
                table: "AppTiposCondicionesIva",
                column: "CodigoAfip",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTiposCondicionesIva");
        }
    }
}

