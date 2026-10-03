namespace DIBA_Backend.Dto.Booking
{
    public class CreateBookingDto
    {
        public Guid EventId { get; set; }

        public Guid VenueId { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public string? SpecialRequirements { get; set; }

        public bool AcknowledgementAccepted { get; set; }
    }
}
