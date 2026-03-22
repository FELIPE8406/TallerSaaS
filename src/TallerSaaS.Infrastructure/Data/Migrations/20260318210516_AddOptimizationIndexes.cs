using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallerSaaS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NominaRegistros_TenantId",
                table: "NominaRegistros");

            migrationBuilder.DropIndex(
                name: "IX_NominaRegistros_TenantId_Periodo",
                table: "NominaRegistros");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Tenant_DateRange_Mechanic",
                table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "NominaRegistros",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MechanicId",
                table: "Appointments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_NominaRegistros_Tenant_Period_Status_User",
                table: "NominaRegistros",
                columns: new[] { "TenantId", "Periodo", "Estado", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Tenant_Mechanic_Dates",
                table: "Appointments",
                columns: new[] { "TenantId", "MechanicId", "StartDateTime", "EndDateTime" })
                .Annotation("SqlServer:Include", new[] { "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NominaRegistros_Tenant_Period_Status_User",
                table: "NominaRegistros");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Tenant_Mechanic_Dates",
                table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "NominaRegistros",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "MechanicId",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_NominaRegistros_TenantId",
                table: "NominaRegistros",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NominaRegistros_TenantId_Periodo",
                table: "NominaRegistros",
                columns: new[] { "TenantId", "Periodo" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Tenant_DateRange_Mechanic",
                table: "Appointments",
                columns: new[] { "TenantId", "StartDateTime", "EndDateTime" })
                .Annotation("SqlServer:Include", new[] { "MechanicId", "Status" });
        }
    }
}
