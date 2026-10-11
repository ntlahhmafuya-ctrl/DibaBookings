using System.Security.Claims;
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

    // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
    // Similar logic: restricting an entire controller to members of a
    // particular application role.
    // DIBA adaptation: all administration endpoints are restricted to
    // users with the Administrator role.
    [Authorize(Roles = "Administrator")]
    public class AdministrationController : ControllerBase
    {
        private readonly DIBABookingsDbContext _dbContext;

        public AdministrationController(DIBABookingsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/Administration/overview
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            // Retrieve recent audit activity for the administration dashboard.
            //
            // Reference: anassrh, "Secure Backend API with ASP.NET Core".
            // Similar logic: sensitive application operations are recorded in
            // audit logs and can be retrieved through protected endpoints.
            // DIBA adaptation: audit records are used here as part of the
            // Administrator dashboard's recent activity section.
            var recentActivity = await _dbContext.AuditLogs
                .OrderByDescending(auditLog => auditLog.Timestamp)
                .Take(8)
                .Select(auditLog => new
                {
                    auditLog.AuditLogId,
                    auditLog.Action,
                    auditLog.LogDescription,
                    auditLog.Timestamp,

                    // DIBA-specific handling: an audit record without an
                    // associated user is displayed as a system action.
                    UserName = auditLog.User == null
                        ? "System"
                        : auditLog.User.FirstName + " " + auditLog.User.LastName
                })
                .ToListAsync();

            // Dashboard statistics are calculated directly from the database
            // rather than being stored as static values.
            //
            // DIBA adaptation: these aggregate queries provide the live
            // numbers displayed on the Administrator dashboard.
            var totalUsers = await _dbContext.Users.CountAsync();

            var activeUsers = await _dbContext.Users
                .CountAsync(user => user.IsActive);

            var totalBookings = await _dbContext.Bookings
                .CountAsync();

            // DIBA-specific booking workflow:
            // bookings are grouped according to their current BookingStatus.
            var pendingBookings = await _dbContext.Bookings
                .CountAsync(booking =>
                    booking.BookingStatus!.StatusName == "Pending");

            var approvedBookings = await _dbContext.Bookings
                .CountAsync(booking =>
                    booking.BookingStatus!.StatusName == "Approved");

            var rejectedBookings = await _dbContext.Bookings
                .CountAsync(booking =>
                    booking.BookingStatus!.StatusName == "Rejected");

            var cancelledBookings = await _dbContext.Bookings
                .CountAsync(booking =>
                    booking.BookingStatus!.StatusName == "Cancelled");

            var totalPayments = await _dbContext.Payments
                .CountAsync();

            return Ok(new
            {
                totalUsers,
                activeUsers,
                totalBookings,
                pendingBookings,
                approvedBookings,
                rejectedBookings,
                cancelledBookings,
                totalPayments,
                recentActivity
            });
        }

        // GET: api/Administration/roles
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _dbContext.Roles
                .OrderBy(role => role.RoleName)

                // Similar EF Core projection pattern:
                // only the fields required by the frontend are selected
                // rather than returning complete entity objects.
                .Select(role => new
                {
                    role.RoleId,
                    role.RoleName
                })
                .ToListAsync();

            return Ok(roles);
        }

        // POST: api/Administration/users
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser(
            CreateManagedUserDto createManagedUserDto)
        {
            // Reference: Olaide Ogunbunmi, "ASP.NET Core Role-Based
            // Authentication API".
            // Similar logic: user-management APIs check whether an account
            // already exists before creating a new user.
            // DIBA adaptation: email is used as the unique user identifier
            // for registration and administration purposes.
            var email = createManagedUserDto.Email.Trim().ToLowerInvariant();
            if (System.Text.Encoding.UTF8.GetByteCount(createManagedUserDto.Password) > 72)
            {
                return BadRequest("Password must not exceed 72 UTF-8 bytes.");
            }

            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(user => user.Email == email);

            if (existingUser != null)
            {
                return Conflict("A user with this email already exists.");
            }

            // DIBA-specific role validation:
            // the administrator must select a role that actually exists
            // in the database before the new user is created.
            var roleExists = await _dbContext.Roles
                .AnyAsync(role =>
                    role.RoleId == createManagedUserDto.RoleId);

            if (!roleExists)
            {
                return BadRequest("Selected role was not found.");
            }

            // Reference: anassrh, "Secure Backend API with ASP.NET Core".
            // Similar logic: user-management systems create accounts using
            // BCrypt password hashing instead of storing the supplied
            // password directly.
            //
            // DIBA adaptation: BCrypt is used when administrators create
            // managed user accounts.
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = createManagedUserDto.FirstName,
                LastName = createManagedUserDto.LastName,
                Email = email,

                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    createManagedUserDto.Password),

                RoleId = createManagedUserDto.RoleId,
                IsActive = true
            };

            _dbContext.Users.Add(user);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (await _dbContext.Users.AsNoTracking().AnyAsync(item => item.Email == email))
                {
                    return Conflict("A user with this email already exists.");
                }

                throw;
            }

            return Ok(new
            {
                userId = user.UserId
            });
        }

        // PUT: api/Administration/users/{id}/status
        [HttpPut("users/{id:guid}/status")]
        public async Task<IActionResult> SetUserStatus(
            Guid id,
            [FromQuery] bool active)
        {
            var user = await _dbContext.Users
                .FindAsync(id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(actorId, out var administratorId))
            {
                return Unauthorized();
            }

            if (!active && id == administratorId)
            {
                return BadRequest("You cannot deactivate your own administrator account.");
            }

            var previousStatus = user.IsActive;
            user.IsActive = active;
            _dbContext.AuditLogs.Add(new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = "User Status Changed",
                LogDescription = $"User {id} active status changed from {previousStatus} to {active}.",
                Timestamp = DateTime.UtcNow,
                UserId = administratorId
            });

            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Administration/users/{id}/role
        [HttpPut("users/{id:guid}/role")]
        public async Task<IActionResult> SetUserRole(
            Guid id,
            UpdateUserRoleDto updateUserRoleDto)
        {
            var user = await _dbContext.Users
                .FindAsync(id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(actorId, out var administratorId))
            {
                return Unauthorized();
            }

            if (id == administratorId)
            {
                return BadRequest("You cannot change your own administrator role.");
            }

            // Reference: Olaide Ogunbunmi, "ASP.NET Core Role-Based
            // Authentication API".
            // Similar logic: role management verifies that a requested role
            // exists before assigning it to a user.
            // DIBA adaptation: the role is represented by RoleId and stored
            // as the user's foreign-key relationship.
            var roleExists = await _dbContext.Roles
                .AnyAsync(role =>
                    role.RoleId == updateUserRoleDto.RoleId);

            if (!roleExists)
            {
                return BadRequest("Selected role was not found.");
            }

            // DIBA-specific role assignment:
            // update the user's RoleId after confirming that the role exists.
            var previousRoleId = user.RoleId;
            user.RoleId = updateUserRoleDto.RoleId;
            _dbContext.AuditLogs.Add(new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = "User Role Changed",
                LogDescription = $"User {id} role changed from {previousRoleId} to {updateUserRoleDto.RoleId}.",
                Timestamp = DateTime.UtcNow,
                UserId = administratorId
            });

            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}