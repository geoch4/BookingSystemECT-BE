using BookingSystem.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace BookingSystem.Infrastructure.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261011130000_SeparateReferralUnits")]
public partial class SeparateReferralUnits : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("DepartmentId", "Bookings", type: "nvarchar(120)", maxLength: 120, nullable: true);
        migrationBuilder.AddColumn<string>("TreatmentUnitId", "Bookings", type: "nvarchar(120)", maxLength: 120, nullable: true);
        migrationBuilder.Sql("UPDATE [Bookings] SET [DepartmentId] = COALESCE([CareUnitId], [ReferralDepartment]);");
        // Existing source-unit IDs must never be inferred as treatment assignments.
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("DepartmentId", "Bookings");
        migrationBuilder.DropColumn("TreatmentUnitId", "Bookings");
    }
}
