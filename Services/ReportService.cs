using System.Globalization;
using AttendanceReportService.Data;
using AttendanceReportService.Dto;
using AttendanceReportService.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AttendanceReportService.Services
{
    public class ReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Saves received attendance reports to the database.
        /// Ensures all DateTime values are stored as UTC to avoid PostgreSQL errors.
        /// </summary>
        public async Task<(bool Success, string Message)> SaveReportsAsync(ReportRequest request)
        {
            if (request?.Reports == null || request.Reports.Count == 0)
                return (false, "Empty report list");

            var reports = request.Reports;

            var entities = reports
                .Select(r => new AttendanceLog
                {
                    Id = Guid.TryParse(r.Id.ToString(), out var parsedId)
                        ? parsedId
                        : Guid.NewGuid(),

                    UserId = r.UserId,
                    FullName = r.FullName?.Trim(),
                    State = r.State?.Trim(),
                    Lga = r.Lga?.Trim(),
                    Designation = r.Designation,
                    Facility = r.Facility,
                    PhoneNumber = r.PhoneNumber,

                    // ✅ Normalize all date values to UTC
                    CheckInDate = ToUtc(r.CheckInDate),
                    CheckOutDate = ToUtc(r.CheckOutDate),
                    CheckIn = ToUtc(r.CheckIn),
                    CheckOut = ToUtc(r.CheckOut),

                    Message = r.Message,
                    Success = r.Success,
                    ReceivedAt = DateTime.UtcNow,
                })
                .ToList();

            await _context.AttendanceLogs.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            return (true, $"{entities.Count} records saved successfully.");
        }

        /// <summary>
        /// Converts any DateTime? to UTC with correct DateTimeKind.
        /// </summary>
        private static DateTime? ToUtc(DateTime? value)
        {
            if (value == null)
                return null;

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            };
        }

        /// <summary>
        /// Returns facility-level attendance summary.
        /// </summary>
        /// <summary>
        /// Returns facility-level attendance summary.
        /// </summary>
        public async Task<List<object>> GetFacilitySummaryAsync()
        {
            // Get total users per facility from Staff table
            var totalUsersPerFacility = await _context
                .Staff
                .GroupBy(u => u.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    TotalUsers = g.Count()
                })
                .ToListAsync();

            // Get attendance data grouped by facility
            var attendanceData = await _context
                .AttendanceLogs
                .GroupBy(a => a.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    AttendanceTotal = g.Count(),
                    SuccessCount = g.Count(x => x.Success),
                    FailedCount = g.Count(x => !x.Success),
                    LastCheckIn = g.Max(x => x.CheckInDate)
                })
                .ToListAsync();

            // Combine the data: Total from Staff table, attendance stats from AttendanceLogs
            var result = totalUsersPerFacility
                .Select(f =>
                {
                    var attendance = attendanceData.FirstOrDefault(a => a.Facility == f.Facility);
                    return new
                    {
                        Facility = f.Facility,
                        Total = f.TotalUsers, // Total users in facility from Staff table
                        Success = attendance?.SuccessCount ?? 0,
                        Failed = f.TotalUsers - (attendance?.SuccessCount ?? 0), // Total users - successful attendances
                        LastCheckIn = attendance?.LastCheckIn
                    };
                })
                .ToList();

            return result.Cast<object>().ToList();
        }

        // ✅ Existing methods remain unchanged (SaveReportsAsync, ToUtc, GetFacilitySummaryAsync)

        /// <summary>
        /// Retrieves user timesheet records for a specific month and facility.
        /// </summary>
        public async Task<List<AttendanceLog>> GetUserTimesheetAsync(
            Guid userId,
            int year,
            int month
        )
        {
            return await _context
                .AttendanceLogs.Where(a =>
                    a.UserId == userId
                    && a.CheckInDate.HasValue
                    && a.CheckInDate.Value.Year == year
                    && a.CheckInDate.Value.Month == month
                )
                .OrderBy(a => a.CheckInDate)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves facility-wide attendance logs for a given month.
        /// </summary>
        public async Task<List<AttendanceLog>> GetFacilityMonthlyReportAsync(
            string facility,
            int year,
            int month
        )
        {
            return await _context
                .AttendanceLogs.Where(a =>
                    a.Facility == facility
                    && a.CheckInDate.HasValue
                    && a.CheckInDate.Value.Year == year
                    && a.CheckInDate.Value.Month == month
                )
                .OrderBy(a => a.FullName)
                .ThenBy(a => a.CheckInDate)
                .ToListAsync();
        }

        /// <summary>
        /// Generates a PDF timesheet for a specific user and month.
        /// </summary>
        public async Task<byte[]> GenerateUserTimesheetPdfAsync(Guid userId, int year, int month)
        {
            var records = await GetUserTimesheetAsync(userId, year, month);
            if (!records.Any())
                throw new Exception(
                    "No attendance records found for the specified user and month."
                );

            var user = records.First();
            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Header()
                        .Text($"Attendance Timesheet - {user.FullName}")
                        .FontSize(18)
                        .Bold();

                    page.Content()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Date
                                columns.RelativeColumn(2); // Check In
                                columns.RelativeColumn(2); // Check Out
                                columns.RelativeColumn(3); // Message
                                columns.RelativeColumn(1); // Success
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Date").Bold();
                                header.Cell().Text("Check In").Bold();
                                header.Cell().Text("Check Out").Bold();
                                header.Cell().Text("Message").Bold();
                                header.Cell().Text("Status").Bold();
                            });

                            foreach (var log in records)
                            {
                                table.Cell().Text(log.CheckInDate?.ToString("yyyy-MM-dd") ?? "-");
                                table
                                    .Cell()
                                    .Text(log.CheckIn?.ToLocalTime().ToString("HH:mm") ?? "-");
                                table
                                    .Cell()
                                    .Text(log.CheckOut?.ToLocalTime().ToString("HH:mm") ?? "-");
                                table.Cell().Text(log.Message ?? "");
                                table.Cell().Text(log.Success ? "✔️" : "❌");
                            }
                        });

                    page.Footer()
                        .AlignRight()
                        .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            });

            using var stream = new MemoryStream();
            pdf.GeneratePdf(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Returns analytics summary data for charts (e.g. per facility or success/failure rate).
        /// </summary>
        public async Task<object> GetChartAnalyticsAsync(int year, int month)
        {
            // Get attendance data for the specified month
            var attendanceData = await _context
                .AttendanceLogs
                .Where(a =>
                    a.CheckInDate.HasValue
                    && a.CheckInDate.Value.Year == year
                    && a.CheckInDate.Value.Month == month
                )
                .GroupBy(a => a.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    SuccessCount = g.Count(x => x.Success),
                    LastCheckIn = g.Max(x => x.CheckInDate)
                })
                .ToListAsync();

            // Get total users per facility from User table
            var facilityUserCounts = await _context
                .Staff
                .GroupBy(u => u.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    TotalUsers = g.Count()
                })
                .ToListAsync();

            // Combine the data
            var result = facilityUserCounts
                .Select(f => new
                {
                    Facility = f.Facility,
                    Total = f.TotalUsers,
                    Success = attendanceData
                        .FirstOrDefault(a => a.Facility == f.Facility)?.SuccessCount ?? 0,
                    Failed = f.TotalUsers - (attendanceData
                        .FirstOrDefault(a => a.Facility == f.Facility)?.SuccessCount ?? 0),
                    LastCheckIn = attendanceData
                        .FirstOrDefault(a => a.Facility == f.Facility)?.LastCheckIn
                })
                .ToList();

            return result;
        }
    }
}
