using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallerSaaS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehiculos_TenantId",
                table: "Vehiculos");

            migrationBuilder.DropIndex(
                name: "IX_Ordenes_TenantId",
                table: "Ordenes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_TenantId",
                table: "Clientes");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Tenant_Date",
                table: "Vehiculos",
                columns: new[] { "TenantId", "FechaRegistro" });

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_Tenant_Date_State",
                table: "Ordenes",
                columns: new[] { "TenantId", "FechaEntrada", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Tenant_Date",
                table: "Clientes",
                columns: new[] { "TenantId", "FechaRegistro" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehiculos_Tenant_Date",
                table: "Vehiculos");

            migrationBuilder.DropIndex(
                name: "IX_Ordenes_Tenant_Date_State",
                table: "Ordenes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Tenant_Date",
                table: "Clientes");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_TenantId",
                table: "Vehiculos",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_TenantId",
                table: "Ordenes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_TenantId",
                table: "Clientes",
                column: "TenantId");
        }
    }
}
