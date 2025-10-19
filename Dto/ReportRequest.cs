namespace AttendanceReportService.Dto
{
    public class ReportRequest
    {
        public List<AttendanceLogDto> Reports { get; set; } = new();
    }
}
