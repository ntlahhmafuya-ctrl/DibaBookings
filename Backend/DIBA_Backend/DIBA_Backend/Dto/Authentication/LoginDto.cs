using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.Authentication
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        [StringLength(320)]
        [RegularExpression(@".*\\S.*", ErrorMessage = "Email cannot be blank.")]
        public required string Email { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(72)]
        public required string Password { get; set; }
    }
}