using AttendanceReportService.Dto;
using AttendanceReportService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AttendanceReportService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user (Admin only)
        /// </summary>
        /// <param name="dto">User registration data</param>
        /// <response code="200">User registered successfully</response>
        /// <response code="400">Registration failed</response>
        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(
            Summary = "Register a new user (Admin only)",
            Description = "Allows only Admins to create new users."
        )]
        [SwaggerResponse(200, "User registered successfully.")]
        [SwaggerResponse(400, "Failed to register user.")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var (success, message) = await _authService.RegisterAsync(dto);
            return success
                ? Ok(new { status = "success", message })
                : BadRequest(new { status = "error", message });
        }

        /// <summary>
        /// Logs in a user and returns a JWT token
        /// </summary>
        /// <param name="dto">Login credentials</param>
        /// <response code="200">Login successful, returns JWT token</response>
        /// <response code="401">Invalid credentials</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "User login",
            Description = "Authenticates user and returns a JWT token."
        )]
        [SwaggerResponse(200, "Login successful, returns JWT token.")]
        [SwaggerResponse(401, "Invalid credentials.")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var (success, token, user, message) = await _authService.LoginAsync(dto);
            return success
                ? Ok(
                    new
                    {
                        status = "success",
                        token,
                        user,
                        message,
                    }
                )
                : Unauthorized(new { status = "error", message });
        }
    }
}
