using DIBA_Backend.Data;
using DIBA_Backend.Dto.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly DIBABookingsDbContext _dbContext;

        public AccountController(DIBABookingsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // POST: api/Account/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized();
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(existingUser => existingUser.UserId == userGuid);

            if (user == null)
            {
                return Unauthorized();
            }

            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("Current password is incorrect.");
            }

            if (BCrypt.Net.BCrypt.Verify(changePasswordDto.NewPassword, user.PasswordHash))
            {
                return BadRequest("New password must be different from the current password.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Password changed successfully. Please sign in again on other devices."
            });
        }
    }
}