using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.Authentication
{
    public class RegisterUserDto
    {
        [Required, StringLength(80, MinimumLength = 1)]
        public required string FirstName { get; set; }

        [Required, StringLength(80, MinimumLength = 1)]
        public required string LastName { get; set; }

        [Required, EmailAddress, StringLength(320)]
        public required string Email { get; set; }

        [Required, StringLength(72, MinimumLength = 12)]
        public required string Password { get; set; }
    }
}
