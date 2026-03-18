using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallerSaaS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendaIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MechanicAvailabilities_TenantId",
                table: "MechanicAvailabilities");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TenantId",
                table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "MechanicId",
                table: "MechanicAvailabilities",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_MechanicAvailabilities_Mechanic_Day_Active",
                table: "MechanicAvailabilities",
                columns: new[] { "TenantId", "MechanicId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Tenant_DateRange_Mechanic",
                table: "Appointments",
                columns: new[] { "TenantId", "StartDateTime", "EndDateTime" })
                .Annotation("SqlServer:Include", new[] { "MechanicId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MechanicAvailabilities_Mechanic_Day_Active",
                table: "MechanicAvailabilities");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Tenant_DateRange_Mechanic",
                table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "MechanicId",
                table: "MechanicAvailabilities",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_MechanicAvailabilities_TenantId",
                table: "MechanicAvailabilities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TenantId",
                table: "Appointments",
                column: "TenantId");
        }
    }
}
