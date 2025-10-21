using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AttendanceReportService.Data;
using AttendanceReportService.Dto;
using AttendanceReportService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AttendanceReportService.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<User> _hasher = new();

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ✅ Register user
        public async Task<(bool Success, string Message)> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return (false, "User already exists.");

            if (!Enum.TryParse(dto.Role, true, out UserRole role))
                role = UserRole.User;

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Role = role,
                PasswordHash = _hasher.HashPassword(null, dto.Password),
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, "Registration successful.");
        }

        /// <summary>
        /// Authenticates a user and returns JWT + user info.
        /// </summary>
        public async Task<(bool Success, string Token, object User, string Message)> LoginAsync(
            LoginDto dto
        )
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return (false, null, null, "Invalid email or password.");

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return (false, null, null, "Invalid email or password.");

            var (token, userInfo) = GenerateJwtToken(user);
            return (true, token, userInfo, "Login successful.");
        }

        // ✅ JWT Token generator
        private (string Token, object User) GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("role", user.Role.ToString()),
                new Claim("name", user.FullName),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds
            );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            var userInfo = new
            {
                user.Id,
                user.FullName,
                user.Email,
                Role = user.Role.ToString(),
            };

            return (jwt, userInfo);
        }
    }
}
