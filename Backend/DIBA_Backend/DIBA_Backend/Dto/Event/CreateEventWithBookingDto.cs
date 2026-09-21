namespace DIBA_Backend.Dto.Event
{
    public class CreateEventWithBookingDto
    {
        public required string EventName { get; set; }
        public required string EventDescription { get; set; }
        public string? EventType { get; set; }
        public string? EventAttendance { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public Guid VenueId { get; set; }
        public string? SpecialRequirements { get; set; }
    }
}
