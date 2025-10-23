using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AttendanceReportService.Dto;
using AttendanceReportService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceReportService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("sync")]
        [HttpPost("sync")]
        public async Task<IActionResult> SyncUsers([FromBody] List<UserDTO> users)
        {
            var (success, message) = await _userService.SaveUsersAsync(users);

            if (!success)
                return BadRequest(message);

            return Ok(new { message });
        }
    }
}
