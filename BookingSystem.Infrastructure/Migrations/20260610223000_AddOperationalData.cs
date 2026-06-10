// Backend purpose: Applies the database schema change 20260610223000_AddOperationalData.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260610223000_AddOperationalData")]
public partial class AddOperationalData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Existing bookings start without completed clinical assessments.
        migrationBuilder.AddColumn<int>(
            name: "AnesthesiaAssessmentStatus",
            table: "Bookings",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "HealthDeclarationStatus",
            table: "Bookings",
            type: "int",
            nullable: false,
            defaultValue: 0);

        // Existing notifications remain unread until a user explicitly marks them as read.
        migrationBuilder.AddColumn<bool>(
            name: "IsRead",
            table: "Notifications",
            type: "bit",
            nullable: false,
            defaultValue: false);

        // Store one operational staffing snapshot for each treatment day.
        migrationBuilder.CreateTable(
            name: "StaffingStatuses",
            columns: table => new
            {
                StaffingStatusId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                AnesthesiaAvailable = table.Column<int>(type: "int", nullable: false),
                AnesthesiaRequired = table.Column<int>(type: "int", nullable: false),
                NursesAvailable = table.Column<int>(type: "int", nullable: false),
                NursesRequired = table.Column<int>(type: "int", nullable: false),
                DoctorsAvailable = table.Column<int>(type: "int", nullable: false),
                DoctorsRequired = table.Column<int>(type: "int", nullable: false),
                BreakStart = table.Column<TimeSpan>(type: "time", nullable: true),
                BreakEnd = table.Column<TimeSpan>(type: "time", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StaffingStatuses", x => x.StaffingStatusId);
            });

        // Persist the standby queue instead of keeping it as frontend-only sample data.
        migrationBuilder.CreateTable(
            name: "StandbyEntries",
            columns: table => new
            {
                StandbyEntryId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                PatientId = table.Column<int>(type: "int", nullable: false),
                Department = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Urgency = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StandbyEntries", x => x.StandbyEntryId);
                table.ForeignKey(
                    name: "FK_StandbyEntries_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "PatientId",
                    onDelete: ReferentialAction.Cascade);
            });

        // These indexes enforce the one-record-per-day and one-active-entry-per-patient rules.
        migrationBuilder.CreateIndex(
            name: "IX_StaffingStatuses_Date",
            table: "StaffingStatuses",
            column: "Date",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StandbyEntries_PatientId_IsActive",
            table: "StandbyEntries",
            columns: new[] { "PatientId", "IsActive" },
            unique: true,
            filter: "[IsActive] = 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "StaffingStatuses");
        migrationBuilder.DropTable(name: "StandbyEntries");
        migrationBuilder.DropColumn(name: "AnesthesiaAssessmentStatus", table: "Bookings");
        migrationBuilder.DropColumn(name: "HealthDeclarationStatus", table: "Bookings");
        migrationBuilder.DropColumn(name: "IsRead", table: "Notifications");
    }
}
