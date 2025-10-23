using System;

namespace AttendanceReportService.Dto
{
    public class UserDTO
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Designation { get; set; }
        public string Facility { get; set; }
        public string Phone_number { get; set; }
        public string State { get; set; }
        public string Lga { get; set; }
    }
}
