using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.Authentication
{
    public class LoginDto
    {
        [Required, EmailAddress, StringLength(320)]
        public required string Email { get; set; }

        [Required, StringLength(72)]
        public required string Password { get; set; }
    }
}
