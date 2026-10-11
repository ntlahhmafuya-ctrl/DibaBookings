namespace DIBA_Backend.Models.Entities
{
    /// <summary>
    /// Records the result of an attempted notification email without storing its body.
    /// This record intentionally has no cascading foreign keys so operational history
    /// is not silently deleted with a user or notification.
    /// </summary>
    public class EmailDeliveryLog
    {
        public Guid EmailDeliveryLogId { get; set; } = Guid.NewGuid();

        public Guid? UserId { get; set; }

        public Guid? NotificationId { get; set; }

        public required string RecipientEmail { get; set; }

        public required string Subject { get; set; }

        public required string Status { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? SentAtUtc { get; set; }
    }
}
