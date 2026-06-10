// Backend purpose: Applies the database schema change 20260609202643_TreatmentTypeToEnum.
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TreatmentTypeToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_TreatmentTypes_TreatmentTypeId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "TreatmentTypes");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TreatmentTypeId",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "TreatmentTypeId",
                table: "Bookings",
                newName: "TreatmentType");

            migrationBuilder.AlterColumn<string>(
                name: "RoleName",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TreatmentType",
                table: "Bookings",
                newName: "TreatmentTypeId");

            migrationBuilder.AlterColumn<string>(
                name: "RoleName",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "TreatmentTypes",
                columns: table => new
                {
                    TreatmentTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentTypes", x => x.TreatmentTypeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TreatmentTypeId",
                table: "Bookings",
                column: "TreatmentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_TreatmentTypes_TreatmentTypeId",
                table: "Bookings",
                column: "TreatmentTypeId",
                principalTable: "TreatmentTypes",
                principalColumn: "TreatmentTypeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
