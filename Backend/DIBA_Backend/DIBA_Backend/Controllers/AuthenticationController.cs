using DIBA_Backend.Data;
using DIBA_Backend.Dto.Authentication;
using DIBA_Backend.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly DIBABookingsDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthenticationController(
            DIBABookingsDbContext dbContext,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto registerUser)
        {
            // Reference: Olaide Ogunbunmi, "ASP.NET Core Role-Based Authentication API".
            // Similar logic: checking whether a user already exists before
            // creating a new account.
            // DIBA adaptation: the email address is used to determine whether
            // the Event Organiser is already registered.
            // https://github.com/olaideogunbunmi/aspnetcore-auth-rbac
            var email = registerUser.Email.Trim().ToLowerInvariant();
            if (System.Text.Encoding.UTF8.GetByteCount(registerUser.Password) > 72)
            {
                return BadRequest("Password must not exceed 72 UTF-8 bytes.");
            }

            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(user => user.Email == email);

            if (existingUser != null)
            {
                return BadRequest("A user with this email already exists.");
            }


            // Reference: Olaide Ogunbunmi, "ASP.NET Core Role-Based Authentication API".
            // Similar logic: assigning roles to users during account management.
            // DIBA adaptation: newly registered users are automatically assigned
            // the Event Organiser role because this is the default registration role.
            // https://github.com/olaideogunbunmi/aspnetcore-auth-rbac
            var eventOrganiserRole = await _dbContext.Roles
                .FirstOrDefaultAsync(role => role.RoleName == "Event Organiser");

            if (eventOrganiserRole == null)
            {
                return BadRequest("Default role was not found.");
            }


            // Reference: BcryptNet, "BCrypt.Net".
            // Similar logic: hashing a user's password before it is stored.
            // DIBA adaptation: BCrypt.Net is used to create the PasswordHash
            // stored in the DIBA Users table.
            // https://github.com/BcryptNet/bcrypt.net
            var user = new User
            {
                UserId = Guid.NewGuid(),
                FirstName = registerUser.FirstName.Trim(),
                LastName = registerUser.LastName.Trim(),
                Email = email,
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(registerUser.Password),
                RoleId = eventOrganiserRole.RoleId
            };

            _dbContext.Users.Add(user);

            // The database unique index is the final guard against concurrent
            // registrations using the same email address.
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (await _dbContext.Users.AsNoTracking().AnyAsync(user => user.Email == email))
                {
                    return Conflict("A user with this email already exists.");
                }

                throw;
            }

            return Ok(new
            {
                message = "User registered successfully.",
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = eventOrganiserRole.RoleName
            });
        }


        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            // Similar database lookup logic is used in the reviewed
            // ASP.NET Core authentication project by Olaide Ogunbunmi.
            // DIBA adaptation: the user's Role navigation property is also
            // loaded because the role is required when generating the JWT.
            // Reference:
            // https://github.com/olaideogunbunmi/aspnetcore-auth-rbac
            var email = loginDto.Email.Trim().ToLowerInvariant();
            var user = await _dbContext.Users
                .Include(user => user.Role)
                .FirstOrDefaultAsync(user => user.Email == email);

            if (user == null)
            {
                return Unauthorized("Invalid credentials.");
            }


            // DIBA-specific business rule:
            // a user account must be active before login is allowed.
            if (!user.IsActive)
            {
                return Unauthorized("This account has been deactivated.");
            }


            // Reference: BcryptNet, "BCrypt.Net".
            // Similar logic: verifying a password supplied during login
            // against the stored BCrypt password hash.
            // DIBA adaptation: the submitted password is compared with
            // the PasswordHash stored for the selected DIBA user.
            // https://github.com/BcryptNet/bcrypt.net
            if (!BCrypt.Net.BCrypt.Verify(
                    loginDto.Password,
                    user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }


            // DIBA-specific validation:
            // the authenticated user must have an assigned role because
            // the role is included in the JWT.
            if (user.Role == null)
            {
                return BadRequest("User role was not found.");
            }


            // Reference: Olaide Ogunbunmi, "ASP.NET Core Role-Based Authentication API".
            // Similar logic: successful authentication results in a JWT
            // access token being issued to the client.
            // DIBA adaptation: the generated token contains the DIBA user's
            // identity and role.
            // https://github.com/olaideogunbunmi/aspnetcore-auth-rbac
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token = token,
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = user.Role.RoleName
            });
        }


        private string GenerateJwtToken(User user)
        {
            // Reference: Microsoft Learn, "Configure JWT bearer authentication
            // in ASP.NET Core".
            // Similar logic: a symmetric key is used to sign the JWT.
            // DIBA adaptation: the signing key is loaded from configuration
            // rather than being hard-coded in the controller.
            // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"]!));


            // Reference: Microsoft Learn and reviewed JWT implementations.
            // Similar logic: HMAC-SHA256 signing credentials are used to
            // protect the integrity of the generated token.
            // DIBA adaptation: the algorithm is applied using the configured
            // symmetric security key.
            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);


            // Reference: Microsoft Learn, "Claim-based authorization in
            // ASP.NET Core", and Olaide Ogunbunmi's JWT/RBAC project.
            // Similar logic: user information and role information are
            // represented as claims inside the authenticated identity.
            // DIBA adaptation: the claims contain the DIBA UserId, email
            // and RoleName required by the rest of the application.
            // https://github.com/olaideogunbunmi/aspnetcore-auth-rbac
            var claims = new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    user.UserId.ToString()),

                // Reference: common JWT implementation pattern.
                // The JTI provides a unique identifier for the token.
                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                // Reference: Microsoft Learn, "Claim-based authorization
                // in ASP.NET Core".
                // DIBA uses NameIdentifier later to identify the logged-in
                // user when accessing protected resources.
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()),

                // Reference: claim-based authentication pattern.
                // DIBA stores the user's email as the authenticated name.
                new Claim(
                    ClaimTypes.Name,
                    user.Email),

                // Reference: Microsoft Learn, "Role-based authorization
                // in ASP.NET Core".
                // Similar logic: a role is represented as a role claim so
                // ASP.NET Core can perform role-based authorization.
                // DIBA adaptation: the role is taken from the user's
                // Role navigation property.
                new Claim(
                    ClaimTypes.Role,
                    user.Role!.RoleName)
            };


            // Reference: Microsoft Learn and reviewed JWT implementations.
            // Similar logic: a JwtSecurityToken is created using issuer,
            // audience, claims, expiration and signing credentials.
            // DIBA adaptation: the token is configured for the DIBA API
            // and expires after one hour.
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );


            // Similar JWT implementation pattern: converting the generated
            // JwtSecurityToken into the string sent to the client.
            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}