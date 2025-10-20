using AttendanceReportService.Data;
using AttendanceReportService.Dto;
using AttendanceReportService.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceReportService.Services
{
    public class DeviceHealthService
    {
        private readonly AppDbContext _context;

        public DeviceHealthService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Saves or updates the device health status in the database.
        /// </summary>
        public async Task<(bool Success, string Message)> SaveDeviceHealthAsync(DeviceHealthDto dto)
        {
            try
            {
                if (dto == null)
                    return (false, "Invalid device health data");

                var existingRecord = await _context.DeviceHealths.FirstOrDefaultAsync(d =>
                    d.DeviceName == dto.DeviceName && d.Facility == dto.Facility
                );

                if (existingRecord != null)
                {
                    existingRecord.IsOnline = dto.IsOnline;
                    existingRecord.IpAddress = dto.IpAddress;
                    existingRecord.LastChecked = DateTime.UtcNow;
                }
                else
                {
                    await _context.DeviceHealths.AddAsync(
                        new DeviceHealth
                        {
                            Id = Guid.NewGuid(),
                            DeviceName = dto.DeviceName,
                            IsOnline = dto.IsOnline,
                            Facility = dto.Facility,
                            IpAddress = dto.IpAddress,
                            LastChecked = DateTime.UtcNow,
                        }
                    );
                }

                await _context.SaveChangesAsync();
                return (true, "Device health status saved successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error saving device health: {ex.Message}");
            }
        }

        /// <summary>
        /// Marks all devices that haven’t checked in recently as offline.
        /// </summary>
        public async Task<int> MarkOfflineDevicesAsync(int minutesThreshold = 15)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-minutesThreshold);
            var outdated = await _context
                .DeviceHealths.Where(d => d.LastChecked < threshold && d.IsOnline)
                .ToListAsync();

            foreach (var device in outdated)
                device.IsOnline = false;

            if (outdated.Any())
                await _context.SaveChangesAsync();

            return outdated.Count;
        }

        /// <summary>
        /// Retrieves the health status of all devices.
        /// </summary>
        public async Task<List<DeviceHealth>> GetAllStatusesAsync()
        {
            return await _context.DeviceHealths.OrderByDescending(d => d.LastChecked).ToListAsync();
        }
    }
}
