using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.User
{
    public class UpdateUserDto
    {
        [StringLength(100, MinimumLength = 1)]
        [RegularExpression(@".*\S.*", ErrorMessage = "First name cannot be blank.")]
        public string? FirstName { get; set; }

        [StringLength(100, MinimumLength = 1)]
        [RegularExpression(@".*\S.*", ErrorMessage = "Last name cannot be blank.")]
        public string? LastName { get; set; }

        [EmailAddress]
        [StringLength(254)]
        [RegularExpression(@".*\S.*", ErrorMessage = "Email cannot be blank.")]
        public string? Email { get; set; }
    }
}