// Backend purpose: Applies the database schema change 20260604170000_AddReferralMetadataToBookings.
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Infrastructure.Migrations
{
    public partial class AddReferralMetadataToBookings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferralContactName",
                table: "Bookings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralContactPhone",
                table: "Bookings",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralDepartment",
                table: "Bookings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferralContactName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReferralContactPhone",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReferralDepartment",
                table: "Bookings");
        }
    }
}
