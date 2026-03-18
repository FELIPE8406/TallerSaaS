using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallerSaaS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsientosContables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TipoEvento = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsientosContables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CuentasContables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Clase = table.Column<int>(type: "int", nullable: false),
                    EsActiva = table.Column<bool>(type: "bit", nullable: false),
                    PermiteMovimiento = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasContables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LineasAsientosContables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsientoContableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaContableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Debito = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    Credito = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    TerceroId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CentroCostoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasAsientosContables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasAsientosContables_AsientosContables_AsientoContableId",
                        column: x => x.AsientoContableId,
                        principalTable: "AsientosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LineasAsientosContables_Clientes_TerceroId",
                        column: x => x.TerceroId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineasAsientosContables_CuentasContables_CuentaContableId",
                        column: x => x.CuentaContableId,
                        principalTable: "CuentasContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContables_TenantId",
                table: "AsientosContables",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasContables_TenantId_Codigo",
                table: "CuentasContables",
                columns: new[] { "TenantId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineasAsientosContables_AsientoContableId",
                table: "LineasAsientosContables",
                column: "AsientoContableId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasAsientosContables_CuentaContableId",
                table: "LineasAsientosContables",
                column: "CuentaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasAsientosContables_TerceroId",
                table: "LineasAsientosContables",
                column: "TerceroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineasAsientosContables");

            migrationBuilder.DropTable(
                name: "AsientosContables");

            migrationBuilder.DropTable(
                name: "CuentasContables");
        }
    }
}
