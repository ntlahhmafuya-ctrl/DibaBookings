namespace DIBA_Backend.Dto.User
{
    /// <summary>
    /// Carries only the profile fields that a signed-in user may edit.
    /// User ID, role, and account status are deliberately excluded.
    /// </summary>
    public class UpdateUserDto
    {
        // FUNCTION: FirstName - the editable given name.
        public string? FirstName { get; set; }

        // FUNCTION: LastName - the editable family name.
        public string? LastName { get; set; }

        // FUNCTION: Email - the editable account email; the controller checks for duplicates.
        public string? Email { get; set; }
    }
}
