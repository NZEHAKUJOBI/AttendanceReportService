using AttendanceReportService.Dto;
using AttendanceReportService.Models;
using AttendanceReportService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceReportService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        private readonly DeviceHealthService _healthService;

        public HealthController(DeviceHealthService healthService)
        {
            _healthService = healthService;
        }

        /// <summary>
        /// Receives health pings from Java clients and updates device status.
        /// </summary>
        [HttpPost("ping")]
        public async Task<IActionResult> PingDevice([FromBody] DeviceHealth dto)
        {
            try
            {
                var (success, message) = await _healthService.SaveDeviceHealthAsync(
                    new DeviceHealthDto
                    {
                        DeviceName = dto.DeviceName,
                        Facility = dto.Facility,
                        IpAddress = dto.IpAddress,
                        IsOnline = dto.IsOnline,
                        FacilityCode = dto.FacilityCode,
                        FacilityState = dto.FacilityState,
                        FacilityLga = dto.FacilityLga,
                    }
                );

                return success
                    ? Ok(new { status = "success", message })
                    : BadRequest(new { status = "error", message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        /// <summary>
        /// Lists all facilities and device statuses.
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var data = await _healthService.GetAllStatusesAsync();

            var result = data.Select(x => new
            {
                x.Facility,
                x.DeviceName,
                x.IpAddress,
                x.IsOnline,
                x.FacilityCode,
                x.FacilityState,
                x.FacilityLga,
                LastSeen = x.LastChecked.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            });

            return Ok(result);
        }
    }
}
