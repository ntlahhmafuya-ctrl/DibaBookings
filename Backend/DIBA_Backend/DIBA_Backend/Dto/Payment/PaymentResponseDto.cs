namespace DIBA_Backend.Dto.Payment
{
    public class PaymentResponseDto
    {
        public Guid PaymentId { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        public string? ReferenceNumber { get; set; }

        public Guid BookingId { get; set; }

        // This is the status updated by the verified Yoco webhook.
        public string PaymentStatus { get; set; } = "Pending";
    }
}