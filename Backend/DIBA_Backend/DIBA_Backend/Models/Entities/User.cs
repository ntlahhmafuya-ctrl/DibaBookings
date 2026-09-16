namespace DIBA_Backend.Models.Entities
{
    public class User
    {
        public Guid UserId { get; set; }

        public required string FirstName { get; set; } = string.Empty;

        public required string LastName { get; set; } = string.Empty;

        public required string Email { get; set; } = string.Empty;

        public required string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public Guid RoleId { get; set; }

        // Navigation property
        public Role? Role { get; set; }
    }
}
