using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceReportService.Models
{
    [Table("staff")] // optional: sets DB table name
    public class Staff
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid(); // Primary Key, maps to UserDTO.Id
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Designation { get; set; }
        public string Facility { get; set; }
        public string PhoneNumber { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
        public bool IsSynced { get; set; }
    }
}
