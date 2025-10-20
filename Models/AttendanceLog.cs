using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceReportService.Models
{
    [Table("attendance_logs")]
    public class AttendanceLog
    {
        public String Id { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Designation { get; set; }
        public string Facility { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
