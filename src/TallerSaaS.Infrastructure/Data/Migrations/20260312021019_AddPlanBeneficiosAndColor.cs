using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallerSaaS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanBeneficiosAndColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Beneficios",
                table: "PlanesSuscripcion",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "PlanesSuscripcion",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Beneficios", "ColorHex" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Beneficios", "ColorHex" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Beneficios", "ColorHex" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Beneficios",
                table: "PlanesSuscripcion");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "PlanesSuscripcion");
        }
    }
}
