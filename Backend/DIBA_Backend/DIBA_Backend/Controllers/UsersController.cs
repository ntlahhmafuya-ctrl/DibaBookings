using DIBA_Backend.Data;
using DIBA_Backend.Dto.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
    // Similar logic: requiring authentication before allowing access
    // to protected controller actions.
    // DIBA adaptation: all user profile operations require an
    // authenticated user.
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly DIBABookingsDbContext _dbContext;

        public UsersController(DIBABookingsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            // Reference: Microsoft Learn, "Claims-based authorization
            // in ASP.NET Core".
            // Similar logic: obtaining the authenticated user's identity
            // from a claim.
            // DIBA adaptation: the NameIdentifier claim contains the
            // UserId used to retrieve the user's profile.
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            if (!Guid.TryParse(
                    userId,
                    out Guid userGuid))
            {
                return Unauthorized();
            }

            // Similar Entity Framework Core relationship-loading logic:
            // Include is used to load related role information together
            // with the user.
            // DIBA adaptation: the user's Role is required so that the
            // frontend can display the user's role in the profile response.
            var user = await _dbContext.Users
                .Include(user => user.Role)
                .FirstOrDefaultAsync(
                    user => user.UserId == userGuid);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Similar DTO projection/response logic:
            // database entity information is converted into a response
            // object instead of exposing the entity directly.
            // DIBA adaptation: UserResponseDto contains the profile
            // information required by the frontend.
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

        // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
        // Similar logic: restricting an endpoint to a specific application
        // role.
        // DIBA adaptation: only Administrators can retrieve the complete
        // user list for user management.
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetUsers()
        {
            // DIBA-specific administrator functionality:
            // retrieve all users together with their roles for the
            // administrator user-management screen.
            var users = await _dbContext.Users
                .Include(user => user.Role)
                .Select(user => new UserResponseDto
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,

                    // Similar related-entity handling:
                    // the response uses information from the user's
                    // related Role entity.
                    Role = user.Role != null
                        ? user.Role.RoleName
                        : string.Empty,

                    IsActive = user.IsActive
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(
            UpdateUserDto updateUser)
        {
            // Reference: Microsoft Learn, "Claims-based authorization
            // in ASP.NET Core".
            // Similar logic: identifying the authenticated user from
            // the current request's claims.
            // DIBA adaptation: the UserId determines which profile
            // can be updated.
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized();
            }

            if (!Guid.TryParse(
                    userId,
                    out Guid userGuid))
            {
                return Unauthorized();
            }

            // DIBA-specific ownership logic:
            // retrieve only the profile belonging to the logged-in user.
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(
                    user => user.UserId == userGuid);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Partial-update logic:
            // only fields supplied by the frontend are changed.
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
                // Similar data-integrity logic:
                // check whether another user already has the requested
                // email address before changing the current user's email.
                //
                // DIBA adaptation: UserId != userGuid excludes the
                // current user from the duplicate check.
                var existingUser =
                    await _dbContext.Users
                        .FirstOrDefaultAsync(user =>
                            user.Email ==
                                updateUser.Email &&
                            user.UserId != userGuid);

                if (existingUser != null)
                {
                    return BadRequest(
                        "A user with this email already exists.");
                }

                user.Email = updateUser.Email;
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Profile updated successfully."
            });
        }
    }
}