using System;

namespace DIBA_Backend.Models.Entities
{
    /// <summary>
    /// Stores successfully processed Yoco webhook delivery IDs.
    /// WebhookId is the primary key so duplicate and concurrent deliveries
    /// cannot apply the same event more than once.
    /// </summary>
    public class ProcessedYocoWebhook
    {
        public string WebhookId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime ProcessedAtUtc { get; set; }
    }
}
