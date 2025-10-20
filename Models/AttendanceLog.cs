using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceReportService.Models
{
    [Table("attendance_logs")]
    public class AttendanceLog
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        [Column("designation")]
        public string Designation { get; set; }

        [Column("facility")]
        public string Facility { get; set; }

        [Column("phone_number")]
        public string PhoneNumber { get; set; }

        [Column("check_in_date")]
        public DateTime? CheckInDate { get; set; }

        [Column("check_out_date")]
        public DateTime? CheckOutDate { get; set; }

        [Column("check_in")]
        public DateTime? CheckIn { get; set; }

        [Column("check_out")]
        public DateTime? CheckOut { get; set; }

        [Column("message")]
        public string Message { get; set; }

        [Column("success")]
        public bool Success { get; set; }

        [Column("received_at")]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
