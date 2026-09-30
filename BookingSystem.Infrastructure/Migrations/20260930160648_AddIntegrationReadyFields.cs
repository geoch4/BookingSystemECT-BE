using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationReadyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalPatientId",
                table: "Patients",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntegrationStatus",
                table: "Patients",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RegionCode",
                table: "Patients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "Patients",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<string>(
                name: "CareUnitId",
                table: "Bookings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalPatientId",
                table: "Bookings",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntegrationStatus",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RegionCode",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "Bookings",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Manual");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalPatientId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "IntegrationStatus",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "RegionCode",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CareUnitId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ExternalPatientId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IntegrationStatus",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "RegionCode",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "Bookings");
        }
    }
}
