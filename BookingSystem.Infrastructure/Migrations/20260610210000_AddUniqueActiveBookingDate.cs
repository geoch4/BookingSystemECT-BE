// Backend purpose: Applies the database schema change 20260610210000_AddUniqueActiveBookingDate.
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260610210000_AddUniqueActiveBookingDate")]
    public partial class AddUniqueActiveBookingDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalize historical values to the same minute precision used by current booking handlers.
            migrationBuilder.Sql(
                """
                UPDATE [Bookings]
                SET [Date] = DATETIME2FROMPARTS(
                    DATEPART(YEAR, [Date]),
                    DATEPART(MONTH, [Date]),
                    DATEPART(DAY, [Date]),
                    DATEPART(HOUR, [Date]),
                    DATEPART(MINUTE, [Date]),
                    0,
                    0,
                    7);
                """);

            // Preserve the oldest booking and cancel later active duplicates before creating the unique index.
            migrationBuilder.Sql(
                """
                WITH DuplicateBookings AS
                (
                    SELECT
                        [BookingId],
                        ROW_NUMBER() OVER (
                            PARTITION BY [Date]
                            ORDER BY [BookingId]
                        ) AS [DuplicateNumber]
                    FROM [Bookings]
                    WHERE [Status] <> 2
                )
                UPDATE [Bookings]
                SET [Status] = 2
                WHERE [BookingId] IN
                (
                    SELECT [BookingId]
                    FROM DuplicateBookings
                    WHERE [DuplicateNumber] > 1
                );
                """);

            // Cancelled rows are excluded so a released slot can be booked again.
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Date",
                table: "Bookings",
                column: "Date",
                unique: true,
                filter: "[Status] <> 2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_Date",
                table: "Bookings");
        }
    }
}
