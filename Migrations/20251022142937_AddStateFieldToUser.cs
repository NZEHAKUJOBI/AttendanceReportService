using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceReportService.Migrations
{
    /// <inheritdoc />
    public partial class AddStateFieldToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacilityCode",
                table: "DeviceHealths",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FacilityLga",
                table: "DeviceHealths",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FacilityState",
                table: "DeviceHealths",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "lga",
                table: "attendance_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "state",
                table: "attendance_logs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacilityCode",
                table: "DeviceHealths");

            migrationBuilder.DropColumn(
                name: "FacilityLga",
                table: "DeviceHealths");

            migrationBuilder.DropColumn(
                name: "FacilityState",
                table: "DeviceHealths");

            migrationBuilder.DropColumn(
                name: "lga",
                table: "attendance_logs");

            migrationBuilder.DropColumn(
                name: "state",
                table: "attendance_logs");
        }
    }
}
