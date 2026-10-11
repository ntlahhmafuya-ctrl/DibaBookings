using System.ComponentModel.DataAnnotations;

namespace DIBA_Backend.Dto.Authentication
{
    public class ChangePasswordDto
    {
        [Required]
        [MinLength(1)]
        [MaxLength(72)]
        public required string CurrentPassword { get; set; }

        [Required]
        [MinLength(12, ErrorMessage = "New password must be at least 12 characters long.")]
        [MaxLength(72, ErrorMessage = "New password cannot exceed 72 characters.")]
        public required string NewPassword { get; set; }
    }
}