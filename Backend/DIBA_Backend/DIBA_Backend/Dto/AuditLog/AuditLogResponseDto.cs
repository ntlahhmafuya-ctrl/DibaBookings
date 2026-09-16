namespace DIBA_Backend.Dto.AuditLog
{
    public class AuditLogResponseDto
    {
        public Guid AuditLogId { get; set; }

        public string Action { get; set; } = string.Empty;

        public string? LogDescription { get; set; }

        public DateTime Timestamp { get; set; }

        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;
    }
}
