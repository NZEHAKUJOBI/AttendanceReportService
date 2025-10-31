using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using AttendanceReportService.Data;
using AttendanceReportService.Dto;
using AttendanceReportService.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AttendanceReportService.Services
{
    public class ReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Saves received attendance reports to the database.
        /// Ensures all DateTime values are stored as UTC to avoid PostgreSQL errors.
        /// </summary>
        public async Task<(bool Success, string Message)> SaveReportsAsync(ReportRequest request)
        {
            if (request?.Reports == null || request.Reports.Count == 0)
                return (false, "Empty report list");

            var reports = request.Reports;

            var entities = reports
                .Select(r => new AttendanceLog
                {
                    Id = Guid.TryParse(r.Id.ToString(), out var parsedId)
                        ? parsedId
                        : Guid.NewGuid(),

                    UserId = r.UserId,
                    FullName = r.FullName?.Trim(),
                    State = r.State?.Trim(),
                    Lga = r.Lga?.Trim(),
                    Designation = r.Designation,
                    Facility = r.Facility,
                    PhoneNumber = r.PhoneNumber,

                    // ✅ Normalize all date values to UTC
                    CheckInDate = ToUtc(r.CheckInDate),
                    CheckOutDate = ToUtc(r.CheckOutDate),
                    CheckIn = ToUtc(r.CheckIn),
                    CheckOut = ToUtc(r.CheckOut),

                    Message = r.Message,
                    Success = r.Success,
                    ReceivedAt = DateTime.UtcNow,
                })
                .ToList();

            // Best-effort backfill: if CheckInDate is missing but CheckIn/CheckOut exist, derive the date portion (UTC)
            foreach (var e in entities)
            {
                if (!e.CheckInDate.HasValue)
                {
                    var d = e.CheckIn ?? e.CheckOut;
                    if (d.HasValue)
                    {
                        e.CheckInDate = new DateTime(d.Value.Year, d.Value.Month, d.Value.Day, 0, 0, 0, DateTimeKind.Utc);
                    }
                }
            }

            await _context.AttendanceLogs.AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            return (true, $"{entities.Count} records saved successfully.");
        }

        /// <summary>
        /// Converts any DateTime? to UTC with correct DateTimeKind.
        /// </summary>
        private static DateTime? ToUtc(DateTime? value)
        {
            if (value == null)
                return null;

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            };
        }

        /// <summary>
        /// Returns facility-level attendance summary.
        /// </summary>
        public async Task<List<object>> GetFacilitySummaryAsync()
        {
            // Get total users per facility from Staff table
            var totalUsersPerFacility = await _context
                .Staff
                .GroupBy(u => u.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    TotalUsers = g.Count()
                })
                .ToListAsync();

            // Get attendance data grouped by facility
            var attendanceData = await _context
                .AttendanceLogs
                .GroupBy(a => a.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    AttendanceTotal = g.Count(),
                    SuccessCount = g.Count(x => x.Success),
                    FailedCount = g.Count(x => !x.Success),
                    LastCheckIn = g.Max(x => x.CheckInDate)
                })
                .ToListAsync();

            // Combine the data: Total from Staff table, attendance stats from AttendanceLogs
            var result = totalUsersPerFacility
                .Select(f =>
                {
                    var attendance = attendanceData.FirstOrDefault(a => a.Facility == f.Facility);
                    return new
                    {
                        Facility = f.Facility,
                        Total = f.TotalUsers, // Total users in facility from Staff table
                        Success = attendance?.SuccessCount ?? 0,
                        Failed = f.TotalUsers - (attendance?.SuccessCount ?? 0), // Total users - successful attendances
                        LastCheckIn = attendance?.LastCheckIn
                    };
                })
                .ToList();

            return result.Cast<object>().ToList();
        }

        /// <summary>
        /// Returns facility-level attendance summary for today.
        /// </summary>
        public async Task<List<object>> GetFacilityTodaySummaryAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Get total users per facility from Staff table
            var totalUsersPerFacility = await _context
                .Staff
                .GroupBy(u => u.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    TotalUsers = g.Count()
                })
                .ToListAsync();

            // Get attendance data for today only, grouped by facility
            var attendanceData = await _context
                .AttendanceLogs
                .Where(a => a.CheckInDate.HasValue
                    && a.CheckInDate.Value >= today
                    && a.CheckInDate.Value < tomorrow)
                .GroupBy(a => a.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    AttendanceTotal = g.Count(),
                    SuccessCount = g.Count(x => x.Success),
                    FailedCount = g.Count(x => !x.Success),
                    LastCheckIn = g.Max(x => x.CheckInDate),
                    FirstCheckIn = g.Min(x => x.CheckInDate),
                    UniqueUsersCheckedIn = g.Select(x => x.UserId).Distinct().Count()
                })
                .ToListAsync();

            // Combine the data: Total from Staff table, today's attendance stats from AttendanceLogs
            var result = totalUsersPerFacility
                .Select(f =>
                {
                    var attendance = attendanceData.FirstOrDefault(a => a.Facility == f.Facility);
                    return new
                    {
                        Facility = f.Facility,
                        Date = today.ToString("yyyy-MM-dd"),
                        Total = f.TotalUsers, // Total users in facility from Staff table
                        CheckedIn = attendance?.UniqueUsersCheckedIn ?? 0, // Unique users who checked in today
                        Success = attendance?.SuccessCount ?? 0, // Successful check-ins today
                        Failed = attendance?.FailedCount ?? 0, // Failed check-ins today
                        NotCheckedIn = f.TotalUsers - (attendance?.UniqueUsersCheckedIn ?? 0), // Users who haven't checked in
                        AttendanceRate = f.TotalUsers > 0
                            ? Math.Round(((double)(attendance?.UniqueUsersCheckedIn ?? 0) / f.TotalUsers) * 100, 2)
                            : 0.0, // Attendance percentage
                        FirstCheckIn = attendance?.FirstCheckIn,
                        LastCheckIn = attendance?.LastCheckIn,
                        TotalCheckIns = attendance?.AttendanceTotal ?? 0 // Total check-in attempts (including multiple per user)
                    };
                })
                .OrderByDescending(x => x.AttendanceRate)
                .ToList();

            return result.Cast<object>().ToList();
        }

        // ✅ Existing methods remain unchanged (SaveReportsAsync, ToUtc, GetFacilitySummaryAsync)

        /// <summary>
        /// Retrieves user timesheet records for a specific month and facility.
        /// </summary>
        public async Task<List<AttendanceLog>> GetUserTimesheetAsync(
            Guid userId,
            int year,
            int month
        )
        {
            // First, check if user exists in Staff table
            var userExists = await _context.Staff.AnyAsync(s => s.Id == userId);
            if (!userExists)
            {
                throw new Exception($"User with ID {userId} not found in Staff table");
            }

            // Use a coalesced date (CheckInDate || CheckIn || CheckOut) and filter by UTC month range
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);

            var records = await _context.AttendanceLogs
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .Where(a =>
                    (a.CheckInDate ?? a.CheckIn ?? a.CheckOut) != null &&
                    (a.CheckInDate ?? a.CheckIn ?? a.CheckOut) >= start &&
                    (a.CheckInDate ?? a.CheckIn ?? a.CheckOut) < end
                )
                .OrderBy(a => (a.CheckInDate ?? a.CheckIn ?? a.CheckOut))
                .ToListAsync();

            // Normalize missing CheckInDate for display consistency (does not persist)
            foreach (var rec in records)
            {
                if (!rec.CheckInDate.HasValue)
                {
                    var d = rec.CheckIn ?? rec.CheckOut;
                    if (d.HasValue)
                    {
                        rec.CheckInDate = new DateTime(d.Value.Year, d.Value.Month, d.Value.Day, 0, 0, 0, DateTimeKind.Utc);
                    }
                }
            }

            return records;
        }

        /// <summary>
        /// Retrieves facility-wide attendance logs for a given month.
        /// </summary>
        public async Task<List<AttendanceLog>> GetFacilityMonthlyReportAsync(
            string facility,
            int year,
            int month
        )
        {
            return await _context
                .AttendanceLogs.Where(a =>
                    a.Facility == facility
                    && a.CheckInDate.HasValue
                    && a.CheckInDate.Value.Year == year
                    && a.CheckInDate.Value.Month == month
                )
                .OrderBy(a => a.FullName)
                .ThenBy(a => a.CheckInDate)
                .ToListAsync();
        }

        /// <summary>
        /// Generates a PDF timesheet for a specific user and month.
        /// </summary>
        public async Task<byte[]> GenerateUserTimesheetPdfAsync(Guid userId, int year, int month)
        {
            try
            {
                var records = await GetUserTimesheetAsync(userId, year, month);

                // Resolve user info: first from records, then fallback from Staff
                var userInfo = records.FirstOrDefault() ?? await GetUserInfoFromStaffAsync(userId);
                if (userInfo == null)
                    throw new Exception($"No user found with ID: {userId}");

                // Precompute display values
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
                var total = records.Count;
                var successCount = records.Count(r => r.Success);
                var successRate = total > 0
                    ? Math.Round((double)successCount / total * 100, 1)
                    : 0;

                // Local helpers for formatting and info rows
                static string FmtTime(DateTime? dt) => dt.HasValue ? dt.Value.ToLocalTime().ToString("HH:mm") : "-";
                static string FmtDate(DateTime? dt) => dt.HasValue ? dt.Value.ToString("yyyy-MM-dd") : "-";

                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(40);

                        // Header section
                        page.Header()
                            .Column(column =>
                            {
                                column.Item().Text("Attendance Timesheet")
                                    .FontSize(20)
                                    .Bold()
                                    .AlignCenter();

                                column.Item().Text(userInfo.FullName)
                                    .FontSize(16)
                                    .Bold()
                                    .AlignCenter();

                                column.Item().Text($"{monthName} {year}")
                                    .FontSize(14)
                                    .AlignCenter();

                                column.Item().PaddingTop(10)
                                    .Table(infoTable =>
                                    {
                                        infoTable.ColumnsDefinition(cols =>
                                        {
                                            cols.RelativeColumn();
                                            cols.RelativeColumn();
                                        });

                                        void InfoRow(string label, string? value)
                                        {
                                            infoTable.Cell().Border(1).Padding(5).Text(label).Bold();
                                            infoTable.Cell().Border(1).Padding(5).Text(string.IsNullOrWhiteSpace(value) ? "N/A" : value);
                                        }

                                        InfoRow("Facility:", userInfo.Facility);
                                        InfoRow("Designation:", userInfo.Designation);
                                        InfoRow("State:", userInfo.State);
                                        InfoRow("LGA:", userInfo.Lga);
                                        InfoRow("Phone:", userInfo.PhoneNumber);
                                        InfoRow("Total Records:", total.ToString());
                                        InfoRow("Successful Days:", successCount.ToString());
                                        InfoRow("Success Rate:", $"{successRate}%");
                                    });
                            });

                        // Content section
                        page.Content()
                            .PaddingTop(20)
                            .Column(column =>
                            {
                                if (!records.Any())
                                {
                                    column.Item()
                                        .PaddingTop(50)
                                        .Text("No attendance records found for this period.")
                                        .FontSize(16)
                                        .AlignCenter()
                                        .Italic();
                                }
                                else
                                {
                                    column.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2); // Date
                                            columns.RelativeColumn(2); // Check In
                                            columns.RelativeColumn(2); // Check Out
                                            columns.RelativeColumn(3); // Message
                                            columns.RelativeColumn(1); // Success
                                        });

                                        // Table header
                                        table.Header(header =>
                                        {
                                            header.Cell()
                                                .Background(Colors.Grey.Lighten3)
                                                .Padding(8)
                                                .Text("Date")
                                                .Bold();

                                            header.Cell()
                                                .Background(Colors.Grey.Lighten3)
                                                .Padding(8)
                                                .Text("Check In")
                                                .Bold();

                                            header.Cell()
                                                .Background(Colors.Grey.Lighten3)
                                                .Padding(8)
                                                .Text("Check Out")
                                                .Bold();

                                            header.Cell()
                                                .Background(Colors.Grey.Lighten3)
                                                .Padding(8)
                                                .Text("Message")
                                                .Bold();

                                            header.Cell()
                                                .Background(Colors.Grey.Lighten3)
                                                .Padding(8)
                                                .Text("Status")
                                                .Bold();
                                        });

                                        // Table rows
                                        foreach (var log in records.OrderBy(r => r.CheckInDate ?? r.CheckIn ?? r.CheckOut))
                                        {
                                            var date = (log.CheckInDate ?? log.CheckIn ?? log.CheckOut);
                                            var message = string.IsNullOrWhiteSpace(log.Message)
                                                ? ""
                                                : (log.Message!.Length > 120 ? log.Message.Substring(0, 120) + "…" : log.Message);

                                            table.Cell().Border(1).Padding(5).Text(FmtDate(date));
                                            table.Cell().Border(1).Padding(5).Text(FmtTime(log.CheckIn));
                                            table.Cell().Border(1).Padding(5).Text(FmtTime(log.CheckOut));
                                            table.Cell().Border(1).Padding(5).Text(message);
                                            table.Cell().Border(1).Padding(5).Text(log.Success ? "✔️" : "❌").AlignCenter();
                                        }
                                    });

                                    column.Item().PaddingTop(6).Text("Legend: ✔️ Success, ❌ Failed").FontSize(10).Italic();
                                }
                            });

                        // Footer section
                        page.Footer()
                            .AlignRight()
                            .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                            .FontSize(10);
                    });
                });

                using var stream = new MemoryStream();
                pdf.GeneratePdf(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating user timesheet PDF: {ex.Message}", ex);
            }
        }

        private async Task<AttendanceLog?> GetUserInfoFromStaffAsync(Guid userId)
        {
            return await _context.Staff
                .Where(s => s.Id == userId)
                .Select(s => new AttendanceLog
                {
                    UserId = s.Id,
                    FullName = s.FullName,
                    Designation = s.Designation,
                    Facility = s.Facility,
                    State = s.State,
                    Lga = s.Lga,
                    PhoneNumber = s.PhoneNumber
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Returns analytics summary data for charts (e.g. per facility or success/failure rate).
        /// </summary>
        public async Task<object> GetChartAnalyticsAsync(int year, int month)
        {
            // Get attendance data for the specified month
            var attendanceData = await _context
                .AttendanceLogs
                .Where(a =>
                    a.CheckInDate.HasValue
                    && a.CheckInDate.Value.Year == year
                    && a.CheckInDate.Value.Month == month
                )
                .GroupBy(a => a.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    SuccessCount = g.Count(x => x.Success),
                    LastCheckIn = g.Max(x => x.CheckInDate)
                })
                .ToListAsync();

            // Get total users per facility from User table
            var facilityUserCounts = await _context
                .Staff
                .GroupBy(u => u.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    TotalUsers = g.Count()
                })
                .ToListAsync();

            // Combine the data
            var result = facilityUserCounts
                .Select(f => new
                {
                    Facility = f.Facility,
                    Total = f.TotalUsers,
                    Success = attendanceData
                        .FirstOrDefault(a => a.Facility == f.Facility)?.SuccessCount ?? 0,
                    Failed = f.TotalUsers - (attendanceData
                        .FirstOrDefault(a => a.Facility == f.Facility)?.SuccessCount ?? 0),
                    LastCheckIn = attendanceData
                        .FirstOrDefault(a => a.Facility == f.Facility)?.LastCheckIn
                })
                .ToList();

            return result;
        }

        /// <summary>
        /// Generates a facility-wide timesheet PDF for a specific month.
        /// </summary>
        /// <param name="facility">The facility name</param>
        /// <param name="year">The year</param>
        /// <param name="month">The month</param>
        /// <returns>PDF byte array</returns>
        public async Task<byte[]> GenerateFacilityTimesheetPdfAsync(string facility, int year, int month)
        {
            // Get all users in the facility
            var facilityUsers = await _context.Staff
                .Where(s => s.Facility == facility)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            if (!facilityUsers.Any())
                throw new Exception($"No users found for facility: {facility}");

            // Get attendance records for all users in the facility for the specified month
            var userIds = facilityUsers.Select(u => u.Id).ToList();
            var attendanceRecords = await _context.AttendanceLogs
                .Where(a => userIds.Contains(a.UserId)
                    && a.CheckInDate.HasValue
                    && a.CheckInDate.Value.Year == year
                    && a.CheckInDate.Value.Month == month)
                .OrderBy(a => a.FullName)
                .ThenBy(a => a.CheckInDate)
                .ToListAsync();

            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Header()
                        .Column(column =>
                        {
                            column.Item().Text($"Facility Attendance Report - {facility}")
                                .FontSize(18)
                                .Bold();
                            column.Item().Text($"{monthName} {year}")
                                .FontSize(14);
                            column.Item().Text($"Total Users: {facilityUsers.Count} | " +
                                $"Users with Records: {attendanceRecords.Select(a => a.UserId).Distinct().Count()}")
                                .FontSize(12);
                        });

                    page.Content()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // User Name
                                columns.RelativeColumn(2); // Designation  
                                columns.RelativeColumn(2); // Date
                                columns.RelativeColumn(2); // Check In
                                columns.RelativeColumn(2); // Check Out
                                columns.RelativeColumn(3); // Message
                                columns.RelativeColumn(1); // Status
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("User Name").Bold();
                                header.Cell().Text("Designation").Bold();
                                header.Cell().Text("Date").Bold();
                                header.Cell().Text("Check In").Bold();
                                header.Cell().Text("Check Out").Bold();
                                header.Cell().Text("Message").Bold();
                                header.Cell().Text("Status").Bold();
                            });

                            // Group attendance records by user
                            var groupedRecords = attendanceRecords.GroupBy(a => a.UserId).ToList();

                            foreach (var user in facilityUsers)
                            {
                                var userRecords = groupedRecords.FirstOrDefault(g => g.Key == user.Id);

                                if (userRecords == null || !userRecords.Any())
                                {
                                    // User with no attendance records
                                    table.Cell().Text(user.FullName ?? "");
                                    table.Cell().Text(user.Designation ?? "");
                                    table.Cell().Text("-");
                                    table.Cell().Text("-");
                                    table.Cell().Text("-");
                                    table.Cell().Text("No attendance records");
                                    table.Cell().Text("❌");
                                }
                                else
                                {
                                    // User with attendance records
                                    bool firstRecord = true;
                                    foreach (var record in userRecords.OrderBy(r => r.CheckInDate))
                                    {
                                        table.Cell().Text(firstRecord ? (user.FullName ?? "") : "");
                                        table.Cell().Text(firstRecord ? (user.Designation ?? "") : "");
                                        table.Cell().Text(record.CheckInDate?.ToString("yyyy-MM-dd") ?? "-");
                                        table.Cell().Text(record.CheckIn?.ToLocalTime().ToString("HH:mm") ?? "-");
                                        table.Cell().Text(record.CheckOut?.ToLocalTime().ToString("HH:mm") ?? "-");
                                        table.Cell().Text(record.Message ?? "");
                                        table.Cell().Text(record.Success ? "✔️" : "❌");
                                        firstRecord = false;
                                    }
                                }
                            }
                        });

                    page.Footer()
                        .AlignRight()
                        .Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            });

            using var stream = new MemoryStream();
            pdf.GeneratePdf(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Gets timesheet data for all users in a facility for a specific month.
        /// </summary>
        /// <param name="facility">The facility name</param>
        /// <param name="year">The year</param>
        /// <param name="month">The month</param>
        /// <returns>Facility timesheet data</returns>
        public async Task<object> GetFacilityTimesheetDataAsync(string facility, int year, int month)
        {
            // Get all users in the facility
            var facilityUsers = await _context.Staff
                .Where(s => s.Facility == facility)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            if (!facilityUsers.Any())
                throw new Exception($"No users found for facility: {facility}");

            // Get attendance records for all users in the facility for the specified month
            var userIds = facilityUsers.Select(u => u.Id).ToList();
            var attendanceRecords = await _context.AttendanceLogs
                .Where(a => userIds.Contains(a.UserId)
                    && a.CheckInDate.HasValue
                    && a.CheckInDate.Value.Year == year
                    && a.CheckInDate.Value.Month == month)
                .OrderBy(a => a.FullName)
                .ThenBy(a => a.CheckInDate)
                .ToListAsync();

            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

            // Group attendance records by user
            var groupedRecords = attendanceRecords.GroupBy(a => a.UserId).ToList();

            var userTimesheets = facilityUsers.Select(user =>
            {
                var userRecords = groupedRecords.FirstOrDefault(g => g.Key == user.Id);

                return new
                {
                    User = new
                    {
                        user.Id,
                        user.FullName,
                        user.Designation,
                        user.Facility,
                        user.PhoneNumber,
                        user.State,
                        user.Lga
                    },
                    AttendanceRecords = userRecords?.Select(record => new
                    {
                        record.CheckInDate,
                        record.CheckIn,
                        record.CheckOut,
                        record.Message,
                        record.Success
                    }).OrderBy(r => r.CheckInDate).ToList(),
                    Summary = new
                    {
                        TotalDays = userRecords?.Count() ?? 0,
                        SuccessfulDays = userRecords?.Count(r => r.Success) ?? 0,
                        FailedDays = userRecords?.Count(r => !r.Success) ?? 0
                    }
                };
            }).ToList();

            return new
            {
                Facility = facility,
                Year = year,
                Month = month,
                MonthName = monthName,
                TotalUsers = facilityUsers.Count,
                UsersWithRecords = groupedRecords.Count,
                UsersWithoutRecords = facilityUsers.Count - groupedRecords.Count,
                UserTimesheets = userTimesheets,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }
}
