using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.User
{
    /// <summary>
    /// Carries only the profile fields that a signed-in user may edit.
    /// User ID, role, and account status are deliberately excluded.
    /// </summary>
    public class UpdateUserDto
    {
        [StringLength(80, MinimumLength = 1)]
        [RegularExpression(@"\S", ErrorMessage = "First name cannot be blank.")]
        public string? FirstName { get; set; }

        [StringLength(80, MinimumLength = 1)]
        [RegularExpression(@"\S", ErrorMessage = "Last name cannot be blank.")]
        public string? LastName { get; set; }

        [EmailAddress]
        [StringLength(320)]
        [RegularExpression(@"\S", ErrorMessage = "Email cannot be blank.")]
        public string? Email { get; set; }
    }
}