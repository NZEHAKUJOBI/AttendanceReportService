using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AttendanceReportService.Models;
using AttendanceReportService.Services;
using AttendanceReportService.Dto;
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
        [SwaggerOperation(Summary = "Receive attendance reports from clients")]    
        public async Task<IActionResult> ReceiveReport([FromBody] List<AttendanceLogDto> reports)
        {
            (bool success, string message) = await _reportService.SaveReportsAsync(reports);

            if (!success)
                return BadRequest(new { status = "failed", message });

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
