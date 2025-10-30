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
    }
}
