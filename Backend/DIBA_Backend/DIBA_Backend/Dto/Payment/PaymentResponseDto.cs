namespace DIBA_Backend.Dto.Payment
{
    public class PaymentResponseDto
    {
        public Guid PaymentId { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        public string? ReferenceNumber { get; set; }

        public Guid BookingId { get; set; }

        public string PaymentStatus { get; set; } = "Pending";

        public decimal? RefundAmount { get; set; }

        public string? RefundReason { get; set; }

        public string? RefundStatus { get; set; }

        public DateTime? RefundRequestedAtUtc { get; set; }

        public DateTime? RefundProcessedAtUtc { get; set; }

        public string? YocoRefundId { get; set; }

        public string? RefundFailureReason { get; set; }
    }
}
