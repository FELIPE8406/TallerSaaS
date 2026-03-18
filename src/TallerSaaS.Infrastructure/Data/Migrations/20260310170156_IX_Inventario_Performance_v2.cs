using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallerSaaS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IX_Inventario_Performance_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventario_TenantId",
                table: "Inventario");

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_SKU",
                table: "Inventario",
                column: "SKU");

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_TenantId_Activo",
                table: "Inventario",
                columns: new[] { "TenantId", "Activo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventario_SKU",
                table: "Inventario");

            migrationBuilder.DropIndex(
                name: "IX_Inventario_TenantId_Activo",
                table: "Inventario");

            migrationBuilder.CreateIndex(
                name: "IX_Inventario_TenantId",
                table: "Inventario",
                column: "TenantId");
        }
    }
}
