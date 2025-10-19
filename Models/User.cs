using System;

namespace AttendanceReportService.Models
{
    enum UserRole
    {
        Admin,
        Adhoc,
        Officer,
        Compliance, 
    }

    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } // e.g., "Admin", "User"
    }
}
