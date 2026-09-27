namespace DIBA_Backend.Dto.Venue
{
    public class UpdateVenueDto
    {
        public required string VenueName { get; set; }

        public string VenueDescription { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public decimal Price { get; set; }

        public string Location { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string VenueStatus { get; set; } = string.Empty;
    }
}