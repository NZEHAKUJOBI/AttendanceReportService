using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceReportService.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceHealthTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Success",
                table: "attendance_logs",
                newName: "success"
            );

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "attendance_logs",
                newName: "message"
            );

            migrationBuilder.RenameColumn(
                name: "Facility",
                table: "attendance_logs",
                newName: "facility"
            );

            migrationBuilder.RenameColumn(
                name: "Designation",
                table: "attendance_logs",
                newName: "designation"
            );

            migrationBuilder.RenameColumn(name: "Id", table: "attendance_logs", newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "attendance_logs",
                newName: "user_id"
            );

            migrationBuilder.RenameColumn(
                name: "ReceivedAt",
                table: "attendance_logs",
                newName: "received_at"
            );

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "attendance_logs",
                newName: "phone_number"
            );

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "attendance_logs",
                newName: "full_name"
            );

            migrationBuilder.RenameColumn(
                name: "CheckOutDate",
                table: "attendance_logs",
                newName: "check_out_date"
            );

            migrationBuilder.RenameColumn(
                name: "CheckOut",
                table: "attendance_logs",
                newName: "check_out"
            );

            migrationBuilder.RenameColumn(
                name: "CheckInDate",
                table: "attendance_logs",
                newName: "check_in_date"
            );

            migrationBuilder.RenameColumn(
                name: "CheckIn",
                table: "attendance_logs",
                newName: "check_in"
            );

            // ✅ Explicit SQL casting fix
            migrationBuilder.Sql(
                @"
        ALTER TABLE attendance_logs 
        ALTER COLUMN id TYPE uuid USING id::uuid;
    "
            );

            migrationBuilder.CreateTable(
                name: "DeviceHealths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceName = table.Column<string>(type: "text", nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    Facility = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    LastChecked = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceHealths", x => x.Id);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DeviceHealths");

            migrationBuilder.RenameColumn(
                name: "success",
                table: "attendance_logs",
                newName: "Success"
            );

            migrationBuilder.RenameColumn(
                name: "message",
                table: "attendance_logs",
                newName: "Message"
            );

            migrationBuilder.RenameColumn(
                name: "facility",
                table: "attendance_logs",
                newName: "Facility"
            );

            migrationBuilder.RenameColumn(
                name: "designation",
                table: "attendance_logs",
                newName: "Designation"
            );

            migrationBuilder.RenameColumn(name: "id", table: "attendance_logs", newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "attendance_logs",
                newName: "UserId"
            );

            migrationBuilder.RenameColumn(
                name: "received_at",
                table: "attendance_logs",
                newName: "ReceivedAt"
            );

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "attendance_logs",
                newName: "PhoneNumber"
            );

            migrationBuilder.RenameColumn(
                name: "full_name",
                table: "attendance_logs",
                newName: "FullName"
            );

            migrationBuilder.RenameColumn(
                name: "check_out_date",
                table: "attendance_logs",
                newName: "CheckOutDate"
            );

            migrationBuilder.RenameColumn(
                name: "check_out",
                table: "attendance_logs",
                newName: "CheckOut"
            );

            migrationBuilder.RenameColumn(
                name: "check_in_date",
                table: "attendance_logs",
                newName: "CheckInDate"
            );

            migrationBuilder.RenameColumn(
                name: "check_in",
                table: "attendance_logs",
                newName: "CheckIn"
            );

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "attendance_logs",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid"
            );
        }
    }
}
