using AttendanceReportService.Data;
using AttendanceReportService.Dto;
using AttendanceReportService.Models;
using Microsoft.EntityFrameworkCore;

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
                    Id = Guid.TryParse(r.Id?.ToString(), out var parsedId)
                        ? parsedId
                        : Guid.NewGuid(),

                    UserId = r.UserId,
                    FullName = r.FullName?.Trim(),
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
        public async Task<List<object>> GetFacilitySummaryAsync()
        {
            var result = await _context
                .AttendanceLogs.GroupBy(a => a.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    Total = g.Count(),
                    Success = g.Count(x => x.Success),
                    Failed = g.Count(x => !x.Success),
                    LastCheckIn = g.Max(x => x.CheckInDate),
                })
                .ToListAsync();

            return result.Cast<object>().ToList();
        }
    }
}
