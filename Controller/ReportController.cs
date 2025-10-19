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
            if (request == null || request.Reports == null || !request.Reports.Any())
                return BadRequest(new { status = "error", message = "Empty report list" });

            var (success, message) = await _reportService.SaveReportsAsync(request);

            if (!success)
                return BadRequest(new { status = "error", message });

            return Ok(new { status = "success", message });
        }

        [HttpGet("facility-summary")]
        public async Task<IActionResult> GetFacilitySummary()
        {
            var summary = await _reportService.GetFacilitySummaryAsync();
            return Ok(summary);
        }
    }
}
