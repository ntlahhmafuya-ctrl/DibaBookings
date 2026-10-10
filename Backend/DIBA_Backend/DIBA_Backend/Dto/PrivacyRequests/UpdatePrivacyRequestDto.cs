namespace DIBA_Backend.Dto.PrivacyRequests
{
    /// <summary>
    /// Administrator input for changing a privacy request's workflow status and recording a response.
    /// </summary>
    public class UpdatePrivacyRequestDto
    {
        // FUNCTION: Status - the next allowed workflow state.
        public string Status { get; set; } = string.Empty;

        // FUNCTION: Response - optional explanation or next steps for the requester.
        public string? Response { get; set; }
    }
}