using DIBA_Backend.Data;
using DIBA_Backend.Dto.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly DIBABookingsDbContext dbContext;

        public UsersController(DIBABookingsDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return Unauthorized();
            }

            var user = await dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userGuid);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            var response = new UserResponseDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role?.RoleName ?? string.Empty
            };

            return Ok(response);
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await dbContext.Users
                .Include(u => u.Role)
                .Select(u => new UserResponseDto
                {
                    UserId = u.UserId,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    Role = u.Role != null
                        ? u.Role.RoleName
                        : string.Empty,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(UpdateUserDto updateUser)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return Unauthorized();
            }

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == userGuid);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (updateUser.FirstName != null)
            {
                user.FirstName = updateUser.FirstName;
            }

            if (updateUser.LastName != null)
            {
                user.LastName = updateUser.LastName;
            }

            if (updateUser.Email != null)
            {
                user.Email = updateUser.Email;
            }

            await dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully."
            });
        }
    }
}
