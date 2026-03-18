using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallerSaaS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorizacionCompleta_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoQR",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "FirmadaDigitalmente",
                table: "Facturas");

            migrationBuilder.AddColumn<Guid>(
                name: "BodegaId",
                table: "Inventario",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bodegas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ubicacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bodegas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosInventario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BodegaOrigenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BodegaDestinoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BodegaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosInventario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_Bodegas_BodegaDestinoId",
                        column: x => x.BodegaDestinoId,
                        principalTable: "Bodegas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_Bodegas_BodegaId",
                        column: x => x.BodegaId,
                        principalTable: "Bodegas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_Bodegas_BodegaOrigenId",
                        column: x => x.BodegaOrigenId,
                        principalTable: "Bodegas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_Inventario_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Inventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_BodegaId",
                table: "Inventario",
                column: "BodegaId");

            migrationBuilder.CreateIndex(
                name: "IX_Bodegas_TenantId",
                table: "Bodegas",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_ProductoId",
                table: "MovimientosInventario",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_TenantId",
                table: "MovimientosInventario",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_BodegaDestinoId",
                table: "MovimientosInventario",
                column: "BodegaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_BodegaId",
                table: "MovimientosInventario",
                column: "BodegaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_BodegaOrigenId",
                table: "MovimientosInventario",
                column: "BodegaOrigenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventario_Bodegas_BodegaId",
                table: "Inventario",
                column: "BodegaId",
                principalTable: "Bodegas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventario_Bodegas_BodegaId",
                table: "Inventario");

            migrationBuilder.DropTable(
                name: "MovimientosInventario");

            migrationBuilder.DropTable(
                name: "Bodegas");

            migrationBuilder.DropIndex(
                name: "IX_Inventario_BodegaId",
                table: "Inventario");

            migrationBuilder.DropColumn(
                name: "BodegaId",
                table: "Inventario");

            migrationBuilder.AddColumn<string>(
                name: "CodigoQR",
                table: "Facturas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FirmadaDigitalmente",
                table: "Facturas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
