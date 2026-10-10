namespace DIBA_Backend.Dto.PrivacyRequests
{
    /// <summary>
    /// Input submitted by a signed-in user to request action about their personal information.
    /// The user's identity is taken from the authentication token, not this DTO.
    /// </summary>
    public class CreatePrivacyRequestDto
    {
        // FUNCTION: RequestType - selects the kind of privacy request.
        public string RequestType { get; set; } = string.Empty;

        // FUNCTION: Description - explains the request without requiring sensitive credentials.
        public string Description { get; set; } = string.Empty;
    }
}