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

        /// <summary>
        /// Gets designation analysis grouped by state and designation.
        /// </summary>
        [HttpGet("analysis/designation-by-state")]
        [SwaggerOperation(Summary = "Get designation analysis by state", Description = "Retrieves designation analysis grouped by state and designation")]
        public async Task<IActionResult> GetDesignationAnalysisByState()
        {
            var analysis = await _userService.GetDesignationAnalysisByStateAsync();
            return Ok(analysis);
        }

        /// <summary>
        /// Gets designation analysis grouped by state only.
        /// </summary>
        [HttpGet("analysis/state-breakdown")]
        [SwaggerOperation(Summary = "Get state breakdown analysis", Description = "Retrieves designation analysis grouped by state only")]
        public async Task<IActionResult> GetDesignationAnalysisByStateOnly()
        {
            var analysis = await _userService.GetDesignationAnalysisByStateOnlyAsync();
            return Ok(analysis);
        }

        /// <summary>
        /// Gets designation analysis grouped by designation only.
        /// </summary>
        [HttpGet("analysis/designation-breakdown")]
        [SwaggerOperation(Summary = "Get designation breakdown analysis", Description = "Retrieves designation analysis grouped by designation only")]
        public async Task<IActionResult> GetDesignationAnalysisByDesignationOnly()
        {
            var analysis = await _userService.GetDesignationAnalysisByDesignationOnlyAsync();
            return Ok(analysis);
        }

        /// <summary>
        /// Gets staff contact information with optional filters.
        /// </summary>
        [HttpGet("contacts")]
        [SwaggerOperation(Summary = "Get staff contacts", Description = "Retrieves staff contact information with optional state, designation, and facility filters")]
        public async Task<IActionResult> GetStaffContacts(
            [FromQuery] string? state = null,
            [FromQuery] string? designation = null,
            [FromQuery] string? facility = null)
        {
            var contacts = await _userService.GetStaffContactsAsync(state, designation, facility);
            return Ok(contacts);
        }

        /// <summary>
        /// Gets staff contacts by designation for a specific state.
        /// </summary>
        [HttpGet("contacts/state/{state}")]
        [SwaggerOperation(Summary = "Get staff contacts by state", Description = "Retrieves staff contacts grouped by designation for a specific state")]
        public async Task<IActionResult> GetStaffContactsByDesignationForState(string state)
        {
            var contacts = await _userService.GetStaffContactsByDesignationForStateAsync(state);
            return Ok(contacts);
        }

        /// <summary>
        /// Gets comprehensive staff analysis with contact information.
        /// </summary>
        [HttpGet("analysis/comprehensive")]
        [SwaggerOperation(Summary = "Get comprehensive staff analysis", Description = "Retrieves comprehensive staff analysis including contact coverage and breakdowns")]
        public async Task<IActionResult> GetComprehensiveStaffAnalysis()
        {
            var analysis = await _userService.GetComprehensiveStaffAnalysisAsync();
            return Ok(analysis);
        }
    }
}
