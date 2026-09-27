using DIBA_Backend.Models.Entities;

public class Payment
{
    public Guid PaymentId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? YocoCheckoutId { get; set; }

    public string PaymentStatus { get; set; } = "Pending";

    public Guid BookingId { get; set; }

    public Booking? Booking { get; set; }
}