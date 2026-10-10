namespace DIBA_Backend.Models.Entities
{
    /// <summary>
    /// Stores a user's request about how DIBA Bookings handles their personal information.
    /// Requests are tracked for review; submitting one never deletes account or booking data automatically.
    /// </summary>
    public class PrivacyRequest
    {
        public Guid PrivacyRequestId { get; set; } = Guid.NewGuid();

        // The authenticated account that submitted this request.
        public Guid UserId { get; set; }
        public User? User { get; set; }

        // Examples: Access, Correction, Deletion, Objection, Other.
        public string RequestType { get; set; } = string.Empty;

        // User's explanation. Do not ask users to include passwords or tokens.
        public string Description { get; set; } = string.Empty;

        // Workflow states: Submitted, In Review, Need More Information, Resolved, Rejected.
        public string Status { get; set; } = "Submitted";

        public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        // Staff/admin response explaining the decision or next step.
        public string? Response { get; set; }

        // Administrator who last updated the request, if it has been reviewed.
        public Guid? ReviewedByUserId { get; set; }
    }
}