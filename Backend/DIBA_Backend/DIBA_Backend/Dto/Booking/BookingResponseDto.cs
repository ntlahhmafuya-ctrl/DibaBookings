namespace DIBA_Backend.Dto.Booking
{
    public class BookingResponseDto
    {
        public Guid BookingId { get; set; }

        public DateTime BookingDate { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public string? SpecialRequirements { get; set; }

        public string? AdminNotes { get; set; }

        public Guid UserId { get; set; }

        public Guid EventId { get; set; }

        public Guid VenueId { get; set; }

        public Guid BookingStatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public string OrganiserName { get; set; } = string.Empty;

        public string EventName { get; set; } = string.Empty;

        public string VenueName { get; set; } = string.Empty;
    }
}
