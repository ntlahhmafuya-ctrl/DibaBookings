namespace DIBA_Backend.Dto.Booking
{
    public class VenueAvailabilityDto
    {
        public Guid BookingId { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public string StatusName { get; set; } = string.Empty;
    }
}