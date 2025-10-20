using AttendanceReportService.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceReportService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<DeviceHealth> DeviceHealths { get; set; }
    }
}
