using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.Authentication
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        [StringLength(254)]
        public required string Email { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(72)]
        public required string Password { get; set; }
    }
}