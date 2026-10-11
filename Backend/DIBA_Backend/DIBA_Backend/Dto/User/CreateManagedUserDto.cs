using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.User
{
    public class CreateManagedUserDto
    {
        [Required]
        [StringLength(80, MinimumLength = 1)]
        [RegularExpression(@"\S", ErrorMessage = "First name cannot be blank.")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(80, MinimumLength = 1)]
        [RegularExpression(@"\S", ErrorMessage = "Last name cannot be blank.")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(320)]
        [RegularExpression(@"\S", ErrorMessage = "Email cannot be blank.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(72, MinimumLength = 12)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public Guid RoleId { get; set; }
    }
}