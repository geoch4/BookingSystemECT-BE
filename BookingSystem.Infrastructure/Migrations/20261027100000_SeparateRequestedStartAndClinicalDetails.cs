// Backend purpose: Separates a referral's requested start date from its booked slot, scopes slot
// uniqueness to confirmed bookings per treatment unit, and stores the referral's clinical basis.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027100000_SeparateRequestedStartAndClinicalDetails")]
    public partial class SeparateRequestedStartAndClinicalDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedStartDate",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            // Every existing row was created with its requested day in Date; keep that day as the request.
            migrationBuilder.Sql(
                """
                UPDATE [Bookings]
                SET [RequestedStartDate] = CAST(CAST([Date] AS date) AS datetime2)
                WHERE [RequestedStartDate] IS NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Date",
                table: "Bookings");

            // The old index was stricter (one non-cancelled booking per minute across all units),
            // so no existing confirmed rows can violate the new one.
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TreatmentUnitId_Date",
                table: "Bookings",
                columns: new[] { "TreatmentUnitId", "Date" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateTable(
                name: "ReferralClinicalDetails",
                columns: table => new
                {
                    BookingId = table.Column<int>(type: "int", nullable: false),
                    Indication = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AnesthesiaInfo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SomaticRisk = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FastingConfirmed = table.Column<bool>(type: "bit", nullable: true),
                    BloodPressure = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    WeightKg = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Medications = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RtmsSessionPlan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PreviousTreatments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TreatmentGoal = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContraindicationChecklistComplete = table.Column<bool>(type: "bit", nullable: true),
                    MetalImplantsOrDevices = table.Column<bool>(type: "bit", nullable: true),
                    SeizureHistory = table.Column<bool>(type: "bit", nullable: true),
                    CompletedDocuments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralClinicalDetails", x => x.BookingId);
                    table.ForeignKey(
                        name: "FK_ReferralClinicalDetails_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "BookingId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReferralClinicalDetails");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TreatmentUnitId_Date",
                table: "Bookings");

            // Restoring the global per-minute rule may fail if units now share a confirmed minute.
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Date",
                table: "Bookings",
                column: "Date",
                unique: true,
                filter: "[Status] <> 2");

            migrationBuilder.DropColumn(
                name: "RequestedStartDate",
                table: "Bookings");
        }
    }
}
