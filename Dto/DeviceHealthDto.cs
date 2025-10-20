namespace AttendanceReportService.Dto
{
    public class DeviceHealthDto
    {
        public Guid Id { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public bool IsOnline { get; set; } = true;
        public string Facility { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    }
}
