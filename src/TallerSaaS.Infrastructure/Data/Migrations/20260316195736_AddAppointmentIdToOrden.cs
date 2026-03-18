using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallerSaaS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentIdToOrden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "Ordenes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_AppointmentId",
                table: "Ordenes",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ordenes_Appointments_AppointmentId",
                table: "Ordenes",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ordenes_Appointments_AppointmentId",
                table: "Ordenes");

            migrationBuilder.DropIndex(
                name: "IX_Ordenes_AppointmentId",
                table: "Ordenes");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "Ordenes");
        }
    }
}
