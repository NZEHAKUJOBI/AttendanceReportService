using AttendanceReportService.Dto;
using AttendanceReportService.Models;
using AttendanceReportService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AttendanceReportService.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportsController(ReportService reportService)
        {
            _reportService = reportService;
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
            var pdfBytes = await _reportService.GenerateUserTimesheetPdfAsync(userId, year, month);
            return File(pdfBytes, "application/pdf", $"Timesheet_{userId}_{year}_{month}.pdf");
        }

        [HttpGet("analytics/{year:int}/{month:int}")]
        public async Task<IActionResult> GetChartAnalytics(int year, int month)
        {
            var analytics = await _reportService.GetChartAnalyticsAsync(year, month);
            return Ok(analytics);
        }
    }
}
