using DIBA_Backend.Data;
using DIBA_Backend.Dto.Venue;
using DIBA_Backend.Dto.VenueFeature;
using DIBA_Backend.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
    // Similar logic: protecting an API controller so that only authenticated
    // users can access its endpoints.
    // DIBA adaptation: all venue-related endpoints require authentication,
    // while write operations below have additional role restrictions.
    [Authorize]
    public class VenuesController : ControllerBase
    {
        private readonly DIBABookingsDbContext _dbContext;

        public VenuesController(DIBABookingsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

<<<<<<< HEAD
        // GET: api/Venues
        [HttpGet]
        public async Task<IActionResult> GetVenues()
        {
            var venues = await _dbContext.Venues

                // Reference: Microsoft Learn, Entity Framework Core querying.
                // Similar logic: projecting database records directly into
                // objects containing only the data required by the API response.
                // DIBA adaptation: VenueResponseDto is used so that the API
                // does not expose the complete Venue entity to the frontend.
                .Select(venue => new VenueResponseDto
                {
                    VenueId = venue.VenueId,
                    VenueName = venue.VenueName,
                    VenueDescription = venue.VenueDescription,
                    Capacity = venue.Capacity,
                    Location = venue.Location,
                    VenueStatus = venue.VenueStatus
=======
        [HttpGet]
        public async Task<IActionResult> GetVenues()
        {
            var venues = await dbContext.Venues
                .Select(v => new VenueResponseDto
                {
                    VenueId = v.VenueId,
                    VenueName = v.VenueName,
                    VenueDescription = v.VenueDescription,
                    Capacity = v.Capacity,
                    Price = v.Price,
                    Location = v.Location,
                    Latitude = v.Latitude,
                    Longitude = v.Longitude,
                    VenueStatus = v.VenueStatus
>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc
                })
                .ToListAsync();

            return Ok(venues);
        }

        // GET: api/Venues/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetVenue(Guid id)
        {
            var venue = await _dbContext.Venues
                .FirstOrDefaultAsync(venue => venue.VenueId == id);

            if (venue == null)
            {
                return NotFound("Venue not found.");
            }

            var response = new VenueResponseDto
            {
                VenueId = venue.VenueId,
                VenueName = venue.VenueName,
                VenueDescription = venue.VenueDescription,
                Capacity = venue.Capacity,
                Price = venue.Price,
                Location = venue.Location,
<<<<<<< HEAD
                VenueStatus = venue.VenueStatus
=======
                Latitude = venue.Latitude,
                Longitude = venue.Longitude,
                VenueStatus = venue.VenueStatus 
>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc
            };

            return Ok(response);
        }

        // POST: api/Venues
        [HttpPost]

        // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
        // Similar logic: restricting a write operation to specific roles.
        // DIBA adaptation: venue creation is limited to Administrators and Staff
        // because these users manage the conference centre's venue information.
        [Authorize(Roles = "Administrator,Staff")]
<<<<<<< HEAD
        public async Task<IActionResult> CreateVenue(CreateVenueDto createVenueDto)
=======
        public async Task<IActionResult> CreateVenue(CreateVenueDto createVenueDto) 
>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc
        {
            var venue = new Venue
            {
                VenueId = Guid.NewGuid(),
                VenueName = createVenueDto.VenueName,
                VenueDescription = createVenueDto.VenueDescription,
                Capacity = createVenueDto.Capacity,
<<<<<<< HEAD
                Location = createVenueDto.Location,
                VenueStatus = createVenueDto.VenueStatus
            };

            _dbContext.Venues.Add(venue);

            await _dbContext.SaveChangesAsync();

=======
                Price = createVenueDto.Price,
                Location = createVenueDto.Location,
                Latitude = createVenueDto.Latitude,
                Longitude = createVenueDto.Longitude,
                VenueStatus = createVenueDto.VenueStatus
            };
            dbContext.Venues.Add(venue); 
            await dbContext.SaveChangesAsync();
>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc
            var response = new VenueResponseDto
            {
                VenueId = venue.VenueId,
                VenueName = venue.VenueName,
                VenueDescription = venue.VenueDescription,
                Capacity = venue.Capacity,
<<<<<<< HEAD
                Location = venue.Location,
                VenueStatus = venue.VenueStatus
            };

            // Similar ASP.NET Core API pattern: returning a 201 Created response
            // together with a route to retrieve the newly created resource.
            // DIBA adaptation: GetVenue is used as the retrieval endpoint.
            return CreatedAtAction(
                nameof(GetVenue),
                new { id = venue.VenueId },
                response);
=======
                Price = venue.Price,
                Location = venue.Location,
                Latitude = venue.Latitude,
                Longitude = venue.Longitude,
                VenueStatus = venue.VenueStatus
            };
            return CreatedAtAction(nameof(GetVenue), new { id = venue.VenueId }, response); 
>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc
        }

        // PUT: api/Venues/{id}
        [HttpPut("{id:guid}")]

        // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
        // Similar logic: limiting modification of protected resources to
        // authorised application roles.
        // DIBA adaptation: only Administrators and Staff can modify venues.
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> UpdateVenue(
            Guid id,
            UpdateVenueDto updateVenueDto)
        {
            var venue = await _dbContext.Venues
                .FirstOrDefaultAsync(venue => venue.VenueId == id);

            if (venue == null)
<<<<<<< HEAD
            {
                return NotFound("Venue not found.");
=======
            { 
                return NotFound("Venue not found."); 
>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc
            }

            venue.VenueName = updateVenueDto.VenueName;
            venue.VenueDescription = updateVenueDto.VenueDescription;
            venue.Capacity = updateVenueDto.Capacity;
<<<<<<< HEAD
            venue.Location = updateVenueDto.Location;
=======
            venue.Price = updateVenueDto.Price;
            venue.Location = updateVenueDto.Location;
            venue.Latitude = updateVenueDto.Latitude;
            venue.Longitude = updateVenueDto.Longitude;
>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc
            venue.VenueStatus = updateVenueDto.VenueStatus;

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Venue updated successfully."
            });
        }

        // Venue Features

        // GET: api/Venues/{venueId}/features
        [HttpGet("{venueId:guid}/features")]
        public async Task<IActionResult> GetVenueFeatures(Guid venueId)
        {
            // Similar relationship-validation logic:
            // verify that the parent resource exists before retrieving
            // its related child records.
            // DIBA adaptation: a VenueFeature cannot be requested for
            // a venue that does not exist.
            var venueExists = await _dbContext.Venues
                .AnyAsync(venue => venue.VenueId == venueId);

            if (!venueExists)
            {
                return NotFound("Venue not found.");
            }

            var features = await _dbContext.VenueFeatures

                // Reference: Microsoft Learn, Entity Framework Core querying.
                // Similar logic: filtering related records using a foreign key
                // before projecting the results into response objects.
                // DIBA adaptation: only features belonging to the requested
                // venue are returned.
                .Where(venueFeature => venueFeature.VenueId == venueId)

                .Select(venueFeature => new VenueFeatureResponseDto
                {
                    VenueFeatureId = venueFeature.VenueFeatureId,
                    FeatureName = venueFeature.FeatureName,
                    FeatureDescription = venueFeature.FeatureDescription,
                    FeatureStatus = venueFeature.FeatureStatus,
                    VenueId = venueFeature.VenueId
                })
                .ToListAsync();

            return Ok(features);
        }

        // POST: api/Venues/{venueId}/features
        [HttpPost("{venueId:guid}/features")]

        // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
        // Similar logic: role-based restriction of resource creation.
        // DIBA adaptation: only Administrators and Staff can add features
        // to conference centre venues.
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> CreateVenueFeature(
            Guid venueId,
            CreateVenueFeatureDto createVenueFeatureDto)
        {
            var venueExists = await _dbContext.Venues
                .AnyAsync(venue => venue.VenueId == venueId);

            if (!venueExists)
            {
                return NotFound("Venue not found.");
            }

            var venueFeature = new VenueFeature
            {
                VenueFeatureId = Guid.NewGuid(),
                FeatureName = createVenueFeatureDto.FeatureName,
                FeatureDescription = createVenueFeatureDto.FeatureDescription,
                FeatureStatus = createVenueFeatureDto.FeatureStatus,

                // DIBA-specific parent-child relationship:
                // the new feature is linked to the selected venue
                // through the VenueId foreign key.
                VenueId = venueId
            };

            _dbContext.VenueFeatures.Add(venueFeature);

            await _dbContext.SaveChangesAsync();

            var response = new VenueFeatureResponseDto
            {
                VenueFeatureId = venueFeature.VenueFeatureId,
                FeatureName = venueFeature.FeatureName,
                FeatureDescription = venueFeature.FeatureDescription,
                FeatureStatus = venueFeature.FeatureStatus,
                VenueId = venueFeature.VenueId
            };

            return Ok(response);
        }

        // PUT: api/Venues/{venueId}/features/{featureId}
        [HttpPut("{venueId:guid}/features/{featureId:guid}")]

        // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
        // Similar logic: only authorised roles may modify protected resources.
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> UpdateVenueFeature(
            Guid venueId,
            Guid featureId,
            UpdateVenueFeatureDto updateVenueFeatureDto)
        {
            // Similar parent-child resource validation:
            // both identifiers are checked so that a feature cannot be
            // modified through a different venue's route.
            // DIBA adaptation: the feature must belong to the venue
            // specified in the URL.
            var venueFeature = await _dbContext.VenueFeatures
                .FirstOrDefaultAsync(venueFeature =>
                    venueFeature.VenueFeatureId == featureId &&
                    venueFeature.VenueId == venueId);

            if (venueFeature == null)
            {
                return NotFound("Venue feature not found.");
            }

            venueFeature.FeatureName = updateVenueFeatureDto.FeatureName;
            venueFeature.FeatureDescription = updateVenueFeatureDto.FeatureDescription;
            venueFeature.FeatureStatus = updateVenueFeatureDto.FeatureStatus;

            await _dbContext.SaveChangesAsync();

            var response = new VenueFeatureResponseDto
            {
                VenueFeatureId = venueFeature.VenueFeatureId,
                FeatureName = venueFeature.FeatureName,
                FeatureDescription = venueFeature.FeatureDescription,
                FeatureStatus = venueFeature.FeatureStatus,
                VenueId = venueFeature.VenueId
            };

            return Ok(response);
        }

        // DELETE: api/Venues/{venueId}/features/{featureId}
        [HttpDelete("{venueId:guid}/features/{featureId:guid}")]

        // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
        // Similar logic: restricting deletion of application data to
        // authorised roles.
        // DIBA adaptation: only Administrators and Staff can remove
        // venue features.
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> DeleteVenueFeature(
            Guid venueId,
            Guid featureId)
        {
            // Similar parent-child validation:
            // the query checks both the child ID and its parent ID before
            // allowing the record to be deleted.
            // DIBA adaptation: prevents a feature belonging to another venue
            // from being selected through this endpoint.
            var venueFeature = await _dbContext.VenueFeatures
                .FirstOrDefaultAsync(venueFeature =>
                    venueFeature.VenueFeatureId == featureId &&
                    venueFeature.VenueId == venueId);

            if (venueFeature == null)
            {
                return NotFound("Venue feature not found.");
            }

            _dbContext.VenueFeatures.Remove(venueFeature);

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Venue feature deleted successfully."
            });
        }
    }
}