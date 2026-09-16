namespace DIBA_Backend.Dto.User
{
    public class CreateManagedUserDto
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public Guid RoleId { get; set; }
    }
}