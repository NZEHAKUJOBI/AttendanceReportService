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
                    UserId = u.Id, // Map UserDTO.Id to Staff.UserId
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
                var existingUser = await _context.Staff.FirstOrDefaultAsync(s =>
                    s.UserId == user.UserId
                );

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
    }
}
