using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.User
{
    public class UpdateUserDto
    {
        [StringLength(100, MinimumLength = 1)]
        public string? FirstName { get; set; }

        [StringLength(100, MinimumLength = 1)]
        public string? LastName { get; set; }

        [EmailAddress]
        [StringLength(254)]
        public string? Email { get; set; }
    }
}