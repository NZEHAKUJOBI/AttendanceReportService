using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AttendanceReportService.Dto;
using AttendanceReportService.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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
        [SwaggerOperation(Summary = "Sync users", Description = "Synchronizes user data with the system")]
        public async Task<IActionResult> SyncUsers([FromBody] List<UserDTO> users)
        {
            var (success, message) = await _userService.SaveUsersAsync(users);

            if (!success)
                return BadRequest(message);

            return Ok(new { message });
        }

        /// <summary>
        /// Gets all users.
        /// </summary>
        [HttpGet("all")]
        [SwaggerOperation(Summary = "Get all users", Description = "Retrieves all users from the system")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// Gets all users for a specific facility.
        /// </summary>
        [HttpGet("facility/{facility}")]
        [SwaggerOperation(Summary = "Get users by facility", Description = "Retrieves all users in a specific facility")]
        public async Task<IActionResult> GetUsersByFacility(string facility)
        {
            var users = await _userService.GetUsersByFacilityAsync(facility);
            return Ok(users);
        }

        /// <summary>
        /// Gets a specific user by ID.
        /// </summary>
        [HttpGet("{userId:guid}")]
        [SwaggerOperation(Summary = "Get user by ID", Description = "Retrieves a specific user by their ID")]
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { status = "error", message = "User not found" });

            return Ok(user);
        }

        /// <summary>
        /// Gets all unique facilities.
        /// </summary>
        [HttpGet("facilities")]
        [SwaggerOperation(Summary = "Get all facilities", Description = "Retrieves all unique facility names")]
        public async Task<IActionResult> GetAllFacilities()
        {
            var facilities = await _userService.GetAllFacilitiesAsync();
            return Ok(facilities);
        }

        /// <summary>
        /// Gets facility summary with user counts.
        /// </summary>
        [HttpGet("facility-summary")]
        [SwaggerOperation(Summary = "Get facility summary", Description = "Retrieves facility summary with user counts and details")]
        public async Task<IActionResult> GetFacilitySummary()
        {
            var summary = await _userService.GetFacilitySummaryAsync();
            return Ok(summary);
        }
    }
}
