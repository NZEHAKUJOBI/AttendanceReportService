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
        /// </summary>
        public async Task<(bool Success, string Message)> SaveReportsAsync(ReportRequest request)
        {
            if (request?.Reports == null || request.Reports.Count == 0)
                return (false, "Empty report list");

            var reports = request.Reports;

            var entities = reports
                .Select(r => new AttendanceLog
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    FullName = r.FullName,
                    Designation = r.Designation,
                    Facility = r.Facility,
                    PhoneNumber = r.PhoneNumber,
                    CheckInDate = r.CheckInDate,
                    CheckOutDate = r.CheckOutDate,
                    CheckIn = r.CheckIn,
                    CheckOut = r.CheckOut,
                    Message = r.Message,
                    Success = r.Success,
                })
                .ToList();

            await _context.AttendanceLogs.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            return (true, $"{entities.Count} records saved successfully.");
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
