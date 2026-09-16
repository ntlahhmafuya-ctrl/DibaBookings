using DIBA_Backend.Data;
using DIBA_Backend.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using DIBA_Backend.Dto.Authentication;


namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {

        private readonly IConfiguration configuration;
        private readonly DIBABookingsDbContext dbContext;

        public AuthenticationController(DIBABookingsDbContext dbContext, IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto registerUser)
        {
            // 1. Check whether email is already registered
            var existingUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == registerUser.Email);

            if (existingUser != null)
            {
                return BadRequest("A user with this email already exists.");
            }

            // 2. Find the default role
            var role = await dbContext.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Event Organiser");

            if (role == null)
            {
                return BadRequest("Default role was not found.");
            }

            // 3. Hash the password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(
                registerUser.Password
            );

            // 4. Create the user
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = registerUser.FirstName,
                LastName = registerUser.LastName,
                Email = registerUser.Email,
                PasswordHash = passwordHash,
                RoleId = role.RoleId
            };

            // 5. Add user to database
            dbContext.Users.Add(user);

            // 6. Save changes
            await dbContext.SaveChangesAsync();

            // 7. Return successful response
            return Ok(new
            {
                message = "User registered successfully.",
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = role.RoleName
            });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            // Find user by email
            var user = await dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                return Unauthorized("Invalid credentials");
            }

            if (!user.IsActive)
            {
                return Unauthorized("This account has been deactivated.");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials");
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token = token,
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = user.Role?.RoleName
            });
        }


        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256    );

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,user.UserId.ToString()),
                
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),

                new Claim(ClaimTypes.Name, user.Email),
               
                new Claim(ClaimTypes.Role, user.Role!.RoleName)
            };

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Authorize]
        [HttpGet("test")]
        public IActionResult TestAuthorization()
        {
            return Ok("You are authenticated.");
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("admin-test")]
        public IActionResult AdminTest()
        {
            return Ok("You are an Administrator.");
        }
    }
}
