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
        public DbSet<Staff> Staff { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ✅ Ensure enum stored as string
            modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();

            // ✅ Optional: unique constraint on Email
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        }
    }
}
