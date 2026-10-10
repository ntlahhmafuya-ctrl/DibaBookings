using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.User
{
    public class CreateManagedUserDto
    {
        [Required, StringLength(80, MinimumLength = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(80, MinimumLength = 1)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(72, MinimumLength = 12)]
        public string Password { get; set; } = string.Empty;

        public Guid RoleId { get; set; }
    }
}