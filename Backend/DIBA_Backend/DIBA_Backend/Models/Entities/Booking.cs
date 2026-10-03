namespace DIBA_Backend.Models.Entities
{
    public class Booking
    {
        public Guid BookingId { get; set; }

        public DateTime BookingDate { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public string? SpecialRequirements { get; set; }
        
        public string? AdminNotes { get; set; }

        public bool AcknowledgementAccepted { get; set; }

        public DateTime? AcknowledgementAcceptedAt { get; set; }

        public Guid UserId { get; set; }

        public Guid EventId { get; set; }

        public Guid VenueId { get; set; }

        public Guid BookingStatusId { get; set; }

        // Navigation properties
        public User? User { get; set; }

        public Event? Event { get; set; }

        public Venue? Venue { get; set; }

        public BookingStatus? BookingStatus { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
