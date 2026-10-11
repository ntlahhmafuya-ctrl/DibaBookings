using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.User
{
    public class CreateManagedUserDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(12, ErrorMessage = "Password must be at least 12 characters long.")]
        [MaxLength(72, ErrorMessage = "Password cannot exceed 72 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public Guid RoleId { get; set; }
    }
}