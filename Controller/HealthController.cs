using AttendanceReportService.Models;
using AttendanceReportService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceReportService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly DeviceHealthService _healthService;

        public HealthController(DeviceHealthService healthService)
        {
            _healthService = healthService;
        }

        [HttpPost("ping")]
        public async Task<IActionResult> PingDevice([FromBody] DeviceHealth dto)
        {
            var (success, message) = await _healthService.SaveDeviceHealthAsync(
                new Dto.DeviceHealthDto
                {
                    DeviceName = dto.DeviceName,
                    Facility = dto.Facility,
                    IpAddress = dto.IpAddress,
                    IsOnline = dto.IsOnline,
                }
            );

            if (!success)
                return BadRequest(new { status = "error", message });

            return Ok(new { status = "success", message });
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var data = await _healthService.GetAllStatusesAsync();
            return Ok(
                data.Select(x => new
                {
                    x.Facility,
                    x.DeviceName,
                    x.IpAddress,
                    x.IsOnline,
                    LastSeen = x.LastChecked.ToLocalTime(),
                })
            );
        }
    }
}
