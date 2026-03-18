using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallerSaaS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturasYTrazabilidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Bloqueada",
                table: "Ordenes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "FacturaId",
                table: "Ordenes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventosTrazabilidad",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehiculoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaEvento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosTrazabilidad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosTrazabilidad_Vehiculos_VehiculoId",
                        column: x => x.VehiculoId,
                        principalTable: "Vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Facturas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroFactura = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    IVA = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CodigoQR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirmadaDigitalmente = table.Column<bool>(type: "bit", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_FacturaId",
                table: "Ordenes",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosTrazabilidad_TenantId",
                table: "EventosTrazabilidad",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosTrazabilidad_VehiculoId",
                table: "EventosTrazabilidad",
                column: "VehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_TenantId",
                table: "Facturas",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ordenes_Facturas_FacturaId",
                table: "Ordenes",
                column: "FacturaId",
                principalTable: "Facturas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ordenes_Facturas_FacturaId",
                table: "Ordenes");

            migrationBuilder.DropTable(
                name: "EventosTrazabilidad");

            migrationBuilder.DropTable(
                name: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Ordenes_FacturaId",
                table: "Ordenes");

            migrationBuilder.DropColumn(
                name: "Bloqueada",
                table: "Ordenes");

            migrationBuilder.DropColumn(
                name: "FacturaId",
                table: "Ordenes");
        }
    }
}
