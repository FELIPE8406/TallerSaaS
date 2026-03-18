using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TallerSaaS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNitAndCiudadToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ciudad",
                table: "Tenants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NIT",
                table: "Tenants",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ciudad",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "NIT",
                table: "Tenants");
        }
    }
}
