using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AttendanceReportService.Data;
using AttendanceReportService.Dto;
using AttendanceReportService.Models; // Your EF models'
using Microsoft.EntityFrameworkCore; // for FirstOrDefaultAsyn

namespace AttendanceReportService.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> SaveUsersAsync(List<UserDTO> users)
        {
            if (users == null || users.Count == 0)
                return (false, "Empty user list");

            var entities = users
                .Select(u => new Staff
                {
                    Id = u.Id, // Map UserDTO.Id to Staff.UserId
                    FullName = u.FullName?.Trim(),
                    Designation = u.Designation,
                    Facility = u.Facility,
                    PhoneNumber = u.Phone_number,
                    State = u.State,
                    Lga = u.Lga,
                    // IsSynced = u.IsSynced   // If you have this property
                })
                .ToList();

            foreach (var user in entities)
            {
                var existingUser = await _context.Staff.FirstOrDefaultAsync(s => s.Id == user.Id);

                if (existingUser == null)
                {
                    _context.Staff.Add(user);
                }
                else
                {
                    // Update existing user properties
                    existingUser.FullName = user.FullName;
                    existingUser.Designation = user.Designation;
                    existingUser.Facility = user.Facility;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.State = user.State;
                    existingUser.Lga = user.Lga;
                }
            }

            await _context.SaveChangesAsync();

            return (true, $"{entities.Count} users saved/updated successfully.");
        }


        public async Task<List<UserDTO>> GetAllUsersAsync()
        {
            var users = await _context.Staff.ToListAsync();

            return users.Select(u => new UserDTO
            {
                Id = u.Id,
                FullName = u.FullName,
                Designation = u.Designation,
                Facility = u.Facility,
                Phone_number = u.PhoneNumber,
                State = u.State,
                Lga = u.Lga,
                // IsSynced = u.IsSynced // If you have this property
            }).ToList();
        }

        /// <summary>
        /// Gets all users for a specific facility.
        /// </summary>
        /// <param name="facility">The facility name</param>
        /// <returns>List of users in the specified facility</returns>
        public async Task<List<UserDTO>> GetUsersByFacilityAsync(string facility)
        {
            var users = await _context.Staff
                .Where(s => s.Facility == facility)
                .ToListAsync();

            return users.Select(u => new UserDTO
            {
                Id = u.Id,
                FullName = u.FullName,
                Designation = u.Designation,
                Facility = u.Facility,
                Phone_number = u.PhoneNumber,
                State = u.State,
                Lga = u.Lga,
                // IsSynced = u.IsSynced // If you have this property
            }).ToList();
        }

        /// <summary>
        /// Gets a specific user by ID.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>User information or null if not found</returns>
        public async Task<UserDTO?> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Staff
                .FirstOrDefaultAsync(s => s.Id == userId);

            if (user == null)
                return null;

            return new UserDTO
            {
                Id = user.Id,
                FullName = user.FullName,
                Designation = user.Designation,
                Facility = user.Facility,
                Phone_number = user.PhoneNumber,
                State = user.State,
                Lga = user.Lga,
                // IsSynced = user.IsSynced // If you have this property
            };
        }

        /// <summary>
        /// Gets all unique facilities from the staff table.
        /// </summary>
        /// <returns>List of facility names</returns>
        public async Task<List<string>> GetAllFacilitiesAsync()
        {
            return await _context.Staff
                .Where(s => !string.IsNullOrEmpty(s.Facility))
                .Select(s => s.Facility)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();
        }

        /// <summary>
        /// Gets facility summary with user counts.
        /// </summary>
        /// <returns>List of facilities with user counts</returns>
        public async Task<List<object>> GetFacilitySummaryAsync()
        {
            var facilitySummary = await _context.Staff
                .Where(s => !string.IsNullOrEmpty(s.Facility))
                .GroupBy(s => s.Facility)
                .Select(g => new
                {
                    Facility = g.Key,
                    UserCount = g.Count(),
                    Users = g.Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Designation
                    }).ToList()
                })
                .OrderBy(f => f.Facility)
                .ToListAsync();

            return facilitySummary.Cast<object>().ToList();
        }

        /// <summary>
        /// Gets designation analysis grouped by state and designation.
        /// </summary>
        /// <returns>Designation analysis data grouped by state and designation</returns>
        public async Task<List<object>> GetDesignationAnalysisByStateAsync()
        {
            var designationAnalysis = await _context.Staff
                .Where(s => !string.IsNullOrEmpty(s.State) && !string.IsNullOrEmpty(s.Designation))
                .GroupBy(s => new { s.State, s.Designation })
                .Select(g => new
                {
                    State = g.Key.State,
                    Designation = g.Key.Designation,
                    Count = g.Count(),
                    Staff = g.Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Facility,
                        u.PhoneNumber,
                        u.Lga
                    }).ToList()
                })
                .OrderBy(x => x.State)
                .ThenBy(x => x.Designation)
                .ToListAsync();

            return designationAnalysis.Cast<object>().ToList();
        }

        /// <summary>
        /// Gets designation analysis grouped by state only.
        /// </summary>
        /// <returns>Designation analysis data grouped by state</returns>
        public async Task<List<object>> GetDesignationAnalysisByStateOnlyAsync()
        {
            var stateAnalysis = await _context.Staff
                .Where(s => !string.IsNullOrEmpty(s.State))
                .GroupBy(s => s.State)
                .Select(g => new
                {
                    State = g.Key,
                    TotalStaff = g.Count(),
                    Designations = g.GroupBy(x => x.Designation)
                        .Select(dg => new
                        {
                            Designation = dg.Key,
                            Count = dg.Count(),
                            Percentage = Math.Round((double)dg.Count() / g.Count() * 100, 2)
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList(),
                    TopDesignation = g.GroupBy(x => x.Designation)
                        .OrderByDescending(dg => dg.Count())
                        .Select(dg => new { Designation = dg.Key, Count = dg.Count() })
                        .FirstOrDefault()
                })
                .OrderBy(x => x.State)
                .ToListAsync();

            return stateAnalysis.Cast<object>().ToList();
        }

        /// <summary>
        /// Gets designation analysis grouped by designation only.
        /// </summary>
        /// <returns>Designation analysis data grouped by designation</returns>
        public async Task<List<object>> GetDesignationAnalysisByDesignationOnlyAsync()
        {
            var designationAnalysis = await _context.Staff
                .Where(s => !string.IsNullOrEmpty(s.Designation))
                .GroupBy(s => s.Designation)
                .Select(g => new
                {
                    Designation = g.Key,
                    TotalStaff = g.Count(),
                    States = g.GroupBy(x => x.State)
                        .Select(sg => new
                        {
                            State = sg.Key,
                            Count = sg.Count(),
                            Percentage = Math.Round((double)sg.Count() / g.Count() * 100, 2)
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList(),
                    TopState = g.GroupBy(x => x.State)
                        .OrderByDescending(sg => sg.Count())
                        .Select(sg => new { State = sg.Key, Count = sg.Count() })
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.TotalStaff)
                .ToListAsync();

            return designationAnalysis.Cast<object>().ToList();
        }

        /// <summary>
        /// Gets staff phone numbers by various filters.
        /// </summary>
        /// <param name="state">Optional state filter</param>
        /// <param name="designation">Optional designation filter</param>
        /// <param name="facility">Optional facility filter</param>
        /// <returns>List of staff with contact information</returns>
        public async Task<List<object>> GetStaffContactsAsync(string? state = null, string? designation = null, string? facility = null)
        {
            var query = _context.Staff.AsQueryable();

            if (!string.IsNullOrEmpty(state))
                query = query.Where(s => s.State == state);

            if (!string.IsNullOrEmpty(designation))
                query = query.Where(s => s.Designation == designation);

            if (!string.IsNullOrEmpty(facility))
                query = query.Where(s => s.Facility == facility);

            var contacts = await query
                .Where(s => !string.IsNullOrEmpty(s.PhoneNumber))
                .Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.PhoneNumber,
                    s.State,
                    s.Designation,
                    s.Facility,
                    s.Lga
                })
                .OrderBy(s => s.State)
                .ThenBy(s => s.FullName)
                .ToListAsync();

            return contacts.Cast<object>().ToList();
        }

        /// <summary>
        /// Gets staff contacts by designation for a specific state.
        /// </summary>
        /// <param name="state">The state to filter by</param>
        /// <returns>Staff contacts grouped by designation for the specified state</returns>
        public async Task<object> GetStaffContactsByDesignationForStateAsync(string state)
        {
            var stateData = await _context.Staff
                .Where(s => s.State == state && !string.IsNullOrEmpty(s.PhoneNumber))
                .GroupBy(s => s.Designation)
                .Select(g => new
                {
                    Designation = g.Key,
                    Count = g.Count(),
                    Contacts = g.Select(s => new
                    {
                        s.Id,
                        s.FullName,
                        s.PhoneNumber,
                        s.Facility,
                        s.Lga
                    }).OrderBy(s => s.FullName).ToList()
                })
                .OrderBy(x => x.Designation)
                .ToListAsync();

            return new
            {
                State = state,
                TotalStaffWithContacts = stateData.Sum(x => x.Count),
                DesignationBreakdown = stateData
            };
        }

        /// <summary>
        /// Gets comprehensive staff analysis with contact information.
        /// </summary>
        /// <returns>Comprehensive staff analysis</returns>
        public async Task<object> GetComprehensiveStaffAnalysisAsync()
        {
            var totalStaff = await _context.Staff.CountAsync();
            var staffWithContacts = await _context.Staff
                .Where(s => !string.IsNullOrEmpty(s.PhoneNumber))
                .CountAsync();

            var stateAnalysis = await _context.Staff
                .GroupBy(s => s.State)
                .Select(g => new
                {
                    State = g.Key,
                    TotalStaff = g.Count(),
                    StaffWithContacts = g.Count(s => !string.IsNullOrEmpty(s.PhoneNumber)),
                    ContactPercentage = Math.Round((double)g.Count(s => !string.IsNullOrEmpty(s.PhoneNumber)) / g.Count() * 100, 2),
                    UniqueDesignations = g.Select(s => s.Designation).Distinct().Count(),
                    UniqueFacilities = g.Select(s => s.Facility).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalStaff)
                .ToListAsync();

            var designationAnalysis = await _context.Staff
                .GroupBy(s => s.Designation)
                .Select(g => new
                {
                    Designation = g.Key,
                    TotalStaff = g.Count(),
                    StaffWithContacts = g.Count(s => !string.IsNullOrEmpty(s.PhoneNumber)),
                    ContactPercentage = Math.Round((double)g.Count(s => !string.IsNullOrEmpty(s.PhoneNumber)) / g.Count() * 100, 2),
                    UniqueStates = g.Select(s => s.State).Distinct().Count(),
                    UniqueFacilities = g.Select(s => s.Facility).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalStaff)
                .ToListAsync();

            return new
            {
                OverallSummary = new
                {
                    TotalStaff = totalStaff,
                    StaffWithContacts = staffWithContacts,
                    ContactCoveragePercentage = Math.Round((double)staffWithContacts / totalStaff * 100, 2),
                    UniqueStates = await _context.Staff.Select(s => s.State).Distinct().CountAsync(),
                    UniqueDesignations = await _context.Staff.Select(s => s.Designation).Distinct().CountAsync(),
                    UniqueFacilities = await _context.Staff.Select(s => s.Facility).Distinct().CountAsync()
                },
                StateBreakdown = stateAnalysis,
                DesignationBreakdown = designationAnalysis,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }
}
