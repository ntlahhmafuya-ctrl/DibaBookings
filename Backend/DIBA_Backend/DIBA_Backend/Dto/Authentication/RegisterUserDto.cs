using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.Authentication
{
    public class RegisterUserDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        [RegularExpression(@".*\S.*", ErrorMessage = "First name cannot be blank.")]
        public required string FirstName { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        [RegularExpression(@".*\S.*", ErrorMessage = "Last name cannot be blank.")]
        public required string LastName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(254)]
        [RegularExpression(@".*\S.*", ErrorMessage = "Email cannot be blank.")]
        public required string Email { get; set; }

        [Required]
        [MinLength(12, ErrorMessage = "Password must be at least 12 characters long.")]
        [MaxLength(72, ErrorMessage = "Password cannot exceed 72 characters.")]
        public required string Password { get; set; }
    }
}