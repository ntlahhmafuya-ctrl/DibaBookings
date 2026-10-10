using DIBA_Backend.Models.Entities;

public class Payment
{
    public Guid PaymentId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? YocoCheckoutId { get; set; }

    public string PaymentStatus { get; set; } = "Pending";

    // Refund tracking is kept with the original payment so DIBA can
    // reconcile one cancellation refund against the Yoco checkout.
    public decimal? RefundAmount { get; set; }

    public string? RefundReason { get; set; }

    public string? RefundStatus { get; set; }

    public DateTime? RefundRequestedAtUtc { get; set; }

    public DateTime? RefundProcessedAtUtc { get; set; }

    public string? YocoRefundId { get; set; }

    public string? RefundFailureReason { get; set; }

    public string? RefundRequestKey { get; set; }

    public Guid BookingId { get; set; }

    public Booking? Booking { get; set; }
}