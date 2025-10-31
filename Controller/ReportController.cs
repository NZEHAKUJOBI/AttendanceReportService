using AttendanceReportService.Data;
using AttendanceReportService.Dto;
using AttendanceReportService.Models;
using AttendanceReportService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AttendanceReportService.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly ReportService _reportService;
        private readonly AppDbContext _context;

        public ReportsController(ReportService reportService, AppDbContext context)
        {
            _reportService = reportService;
            _context = context;
        }

        [HttpPost("receive")]
        public async Task<IActionResult> ReceiveReport([FromBody] ReportRequest request)
        {
            if (request?.Reports == null || !request.Reports.Any())
                return BadRequest(new { status = "error", message = "Empty report list" });

            var (success, message) = await _reportService.SaveReportsAsync(request);

            if (!success)
                return BadRequest(new { status = "error", message });

            return Ok(new { status = "success", message });
        }

        [HttpGet("facility-summary")]
        public async Task<IActionResult> GetFacilitySummary()
        {
            var result = await _reportService.GetFacilitySummaryAsync();
            return Ok(result);
        }

        /// <summary>
        /// Gets detailed facility-level attendance summary for today.
        /// </summary>
        [HttpGet("facility-today-summary")]
        [SwaggerOperation(Summary = "Get today's facility summary", Description = "Returns detailed today's attendance summary for all facilities with attendance rates")]
        public async Task<IActionResult> GetFacilityTodaySummary()
        {
            try
            {
                var result = await _reportService.GetFacilityTodaySummaryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }

        [HttpGet("timesheet/{userId:guid}/{year:int}/{month:int}")]
        public async Task<IActionResult> GetUserTimesheet(Guid userId, int year, int month)
        {
            var data = await _reportService.GetUserTimesheetAsync(userId, year, month);
            return Ok(data);
        }

        [HttpGet("facility/{facility}/{year:int}/{month:int}")]
        public async Task<IActionResult> GetFacilityReport(string facility, int year, int month)
        {
            var data = await _reportService.GetFacilityMonthlyReportAsync(facility, year, month);
            return Ok(data);
        }

        [HttpGet("timesheet-pdf/{userId:guid}/{year:int}/{month:int}")]
        public async Task<IActionResult> GeneratePdf(Guid userId, int year, int month)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateUserTimesheetPdfAsync(userId, year, month);
                var fileName = $"Timesheet_{userId}_{year}_{month:D2}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }

        /// <summary>
        /// Debug endpoint to check user timesheet data before PDF generation.
        /// </summary>
        [HttpGet("debug/timesheet/{userId:guid}/{year:int}/{month:int}")]
        [SwaggerOperation(Summary = "Debug user timesheet data", Description = "Returns raw timesheet data for debugging PDF generation issues")]
        public async Task<IActionResult> DebugUserTimesheet(Guid userId, int year, int month)
        {
            try
            {
                // Get user info from Staff table  
                var userInfo = await _context.Staff
                    .Where(s => s.Id == userId)
                    .Select(s => new
                    {
                        s.Id,
                        s.FullName,
                        s.Designation,
                        s.Facility,
                        s.State,
                        s.Lga,
                        s.PhoneNumber
                    })
                    .FirstOrDefaultAsync();

                if (userInfo == null)
                {
                    return BadRequest(new
                    {
                        status = "error",
                        message = $"User with ID {userId} not found in Staff table",
                        totalStaffCount = await _context.Staff.CountAsync(),
                        sampleStaffIds = await _context.Staff.Take(5).Select(s => s.Id).ToListAsync()
                    });
                }

                // Get ALL attendance records for this user (not filtered by date)
                var allUserRecords = await _context.AttendanceLogs
                    .Where(a => a.UserId == userId)
                    .OrderBy(a => a.CheckInDate)
                    .ToListAsync();

                // Get attendance records for the specific month
                var records = await _reportService.GetUserTimesheetAsync(userId, year, month);

                // Get some general stats
                var totalAttendanceRecords = await _context.AttendanceLogs.CountAsync();
                var totalUniqueUsers = await _context.AttendanceLogs.Select(a => a.UserId).Distinct().CountAsync();

                return Ok(new
                {
                    RequestedParams = new { userId, year, month },
                    UserInfo = userInfo,
                    FilteredAttendanceRecords = records.Select(r => new
                    {
                        r.Id,
                        r.UserId,
                        r.FullName,
                        r.CheckInDate,
                        r.CheckIn,
                        r.CheckOut,
                        r.Message,
                        r.Success,
                        r.Facility,
                        r.Designation
                    }).ToList(),
                    AllUserAttendanceRecords = allUserRecords.Select(r => new
                    {
                        r.Id,
                        r.CheckInDate,
                        CheckInDateInfo = new
                        {
                            Year = r.CheckInDate?.Year,
                            Month = r.CheckInDate?.Month,
                            Day = r.CheckInDate?.Day,
                            Kind = r.CheckInDate?.Kind.ToString()
                        },
                        r.Success,
                        r.Message
                    }).ToList(),
                    Summary = new
                    {
                        RequestedYearMonth = $"{year}-{month:D2}",
                        FilteredRecords = records.Count,
                        AllUserRecords = allUserRecords.Count,
                        SuccessfulFilteredDays = records.Count(r => r.Success),
                        FailedFilteredDays = records.Count(r => !r.Success),
                        DateRangeOfAllRecords = allUserRecords.Any() ? new
                        {
                            From = allUserRecords.Min(r => r.CheckInDate),
                            To = allUserRecords.Max(r => r.CheckInDate)
                        } : null
                    },
                    DatabaseStats = new
                    {
                        TotalAttendanceRecords = totalAttendanceRecords,
                        TotalUniqueUsersWithAttendance = totalUniqueUsers,
                        TotalStaffMembers = await _context.Staff.CountAsync()
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("analytics/{year:int}/{month:int}")]
        public async Task<IActionResult> GetChartAnalytics(int year, int month)
        {
            var analytics = await _reportService.GetChartAnalyticsAsync(year, month);
            return Ok(analytics);
        }

        /// <summary>
        /// Generates a facility-wide timesheet PDF for all users in a facility for a specific month.
        /// </summary>
        [HttpGet("facility-timesheet-pdf/{facility}/{year:int}/{month:int}")]
        [SwaggerOperation(Summary = "Generate facility timesheet PDF", Description = "Creates a PDF timesheet for all users in a facility for the specified month")]
        public async Task<IActionResult> GenerateFacilityTimesheetPdf(string facility, int year, int month)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateFacilityTimesheetPdfAsync(facility, year, month);
                var fileName = $"Facility_Timesheet_{facility}_{year}_{month:D2}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }

        /// <summary>
        /// Gets facility timesheet data for all users in a facility for a specific month.
        /// </summary>
        [HttpGet("facility-timesheet-data/{facility}/{year:int}/{month:int}")]
        [SwaggerOperation(Summary = "Get facility timesheet data", Description = "Retrieves timesheet data for all users in a facility for the specified month")]
        public async Task<IActionResult> GetFacilityTimesheetData(string facility, int year, int month)
        {
            try
            {
                var data = await _reportService.GetFacilityTimesheetDataAsync(facility, year, month);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }

        /// <summary>
        /// Test endpoint to verify QuestPDF is working correctly.
        /// </summary>
        [HttpGet("test-pdf")]
        [SwaggerOperation(Summary = "Test PDF generation", Description = "Generates a simple test PDF to verify QuestPDF is working")]
        public IActionResult TestPdf()
        {
            try
            {
                var pdf = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(40);
                        page.Header()
                            .Text("QuestPDF Test Document")
                            .FontSize(20)
                            .Bold();

                        page.Content()
                            .Column(column =>
                            {
                                column.Item().Text("This is a test PDF document generated by QuestPDF.")
                                    .FontSize(14);
                                column.Item().Text($"Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                                    .FontSize(12);
                                column.Item().Text("If you can see this, QuestPDF is working correctly!")
                                    .FontSize(12)
                                    .Bold();
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text("Test PDF Footer");
                    });
                });

                using var stream = new MemoryStream();
                pdf.GeneratePdf(stream);
                var pdfBytes = stream.ToArray();

                return File(pdfBytes, "application/pdf", "QuestPDF_Test.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }

        /// <summary>
        /// Debug endpoint to check database state and sample data.
        /// </summary>
        [HttpGet("debug/database-state")]
        [SwaggerOperation(Summary = "Check database state", Description = "Returns information about what data exists in the database")]
        public async Task<IActionResult> GetDatabaseState()
        {
            try
            {
                var staffCount = await _context.Staff.CountAsync();
                var attendanceCount = await _context.AttendanceLogs.CountAsync();
                var userCount = await _context.Users.CountAsync();

                var sampleStaff = await _context.Staff
                    .Take(3)
                    .Select(s => new { s.Id, s.FullName, s.Facility, s.Designation })
                    .ToListAsync();

                var sampleAttendance = await _context.AttendanceLogs
                    .Take(3)
                    .ToListAsync();

                var sampleAttendanceFormatted = sampleAttendance.Select(a => new
                {
                    a.Id,
                    a.UserId,
                    a.FullName,
                    a.CheckInDate,
                    a.Facility,
                    CheckInDateInfo = new
                    {
                        Year = a.CheckInDate?.Year,
                        Month = a.CheckInDate?.Month,
                        Day = a.CheckInDate?.Day
                    }
                }).ToList();

                var uniqueUserIdsInAttendance = await _context.AttendanceLogs
                    .Select(a => a.UserId)
                    .Distinct()
                    .Take(5)
                    .ToListAsync();

                return Ok(new
                {
                    DatabaseCounts = new
                    {
                        Staff = staffCount,
                        AttendanceLogs = attendanceCount,
                        Users = userCount
                    },
                    SampleData = new
                    {
                        Staff = sampleStaff,
                        AttendanceLogs = sampleAttendanceFormatted,
                        UniqueUserIdsInAttendance = uniqueUserIdsInAttendance
                    },
                    DatabaseStatus = staffCount > 0 || attendanceCount > 0 ? "Has Data" : "Empty"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message, stackTrace = ex.StackTrace });
            }
        }


    }
}
