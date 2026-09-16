using DIBA_Backend.Data;
using DIBA_Backend.Dto.User;
using DIBA_Backend.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Controllers
{
    [Route("api/Administration")]
    [ApiController]
    [Authorize(Roles = "Administrator")]
    public class AdministrationController : ControllerBase
    {
        private readonly DIBABookingsDbContext dbContext;

        public AdministrationController(DIBABookingsDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var bookings = dbContext.Bookings.Include(b => b.BookingStatus);
            var activity = await dbContext.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Take(8)
                .Select(a => new
                {
                    a.AuditLogId,
                    a.Action,
                    a.LogDescription,
                    a.Timestamp,
                    UserName = a.User == null ? "System" : a.User.FirstName + " " + a.User.LastName
                })
                .ToListAsync();

            return Ok(new
            {
                totalUsers = await dbContext.Users.CountAsync(),
                activeUsers = await dbContext.Users.CountAsync(u => u.IsActive),
                totalBookings = await bookings.CountAsync(),
                pendingBookings = await bookings.CountAsync(b => b.BookingStatus!.StatusName == "Pending"),
                approvedBookings = await bookings.CountAsync(b => b.BookingStatus!.StatusName == "Approved"),
                rejectedBookings = await bookings.CountAsync(b => b.BookingStatus!.StatusName == "Rejected"),
                cancelledBookings = await bookings.CountAsync(b => b.BookingStatus!.StatusName == "Cancelled"),
                totalPayments = await dbContext.Payments.CountAsync(),
                recentActivity = activity
            });
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles() => Ok(await dbContext.Roles
            .OrderBy(r => r.RoleName)
            .Select(r => new { r.RoleId, r.RoleName })
            .ToListAsync());

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser(CreateManagedUserDto dto)
        {
            if (await dbContext.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("A user with this email already exists.");

            if (!await dbContext.Roles.AnyAsync(r => r.RoleId == dto.RoleId))
                return BadRequest("Selected role was not found.");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = dto.RoleId,
                IsActive = true
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            return Ok(new { user.UserId });
        }

        [HttpPut("users/{id:guid}/status")]
        public async Task<IActionResult> SetUserStatus(Guid id, [FromQuery] bool active)
        {
            var user = await dbContext.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");
            user.IsActive = active;
            await dbContext.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("users/{id:guid}/role")]
        public async Task<IActionResult> SetUserRole(Guid id, UpdateUserRoleDto dto)
        {
            var user = await dbContext.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");
            if (!await dbContext.Roles.AnyAsync(r => r.RoleId == dto.RoleId))
                return BadRequest("Selected role was not found.");
            user.RoleId = dto.RoleId;
            await dbContext.SaveChangesAsync();
            return NoContent();
        }
    }
}