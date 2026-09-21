using DIBA_Backend.Data;
using DIBA_Backend.Dto.Venue;
using DIBA_Backend.Dto.VenueFeature;
using DIBA_Backend.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VenuesController : ControllerBase
    {
        private readonly DIBABookingsDbContext dbContext; public VenuesController(DIBABookingsDbContext dbContext) 
        { 
            this.dbContext = dbContext; 
        }

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
                })
                .ToListAsync();

            return Ok(venues);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetVenue(Guid id) 
        { 
            var venue = await dbContext.Venues
                .FirstOrDefaultAsync(v => v.VenueId == id); 

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
                Latitude = venue.Latitude,
                Longitude = venue.Longitude,
                VenueStatus = venue.VenueStatus 
            };
            return Ok(response); 
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> CreateVenue(CreateVenueDto createVenueDto) 
        {
            var venue = new Venue
            {
                VenueId = Guid.NewGuid(),
                VenueName = createVenueDto.VenueName,
                VenueDescription = createVenueDto.VenueDescription,
                Capacity = createVenueDto.Capacity,
                Price = createVenueDto.Price,
                Location = createVenueDto.Location,
                Latitude = createVenueDto.Latitude,
                Longitude = createVenueDto.Longitude,
                VenueStatus = createVenueDto.VenueStatus
            };
            dbContext.Venues.Add(venue); 
            await dbContext.SaveChangesAsync();
            var response = new VenueResponseDto
            {
                VenueId = venue.VenueId,
                VenueName = venue.VenueName,
                VenueDescription = venue.VenueDescription,
                Capacity = venue.Capacity,
                Price = venue.Price,
                Location = venue.Location,
                Latitude = venue.Latitude,
                Longitude = venue.Longitude,
                VenueStatus = venue.VenueStatus
            };
            return CreatedAtAction(nameof(GetVenue), new { id = venue.VenueId }, response); 
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> UpdateVenue(
            Guid id, 
            UpdateVenueDto updateVenueDto) 
        { 
            var venue = await dbContext.Venues.FirstOrDefaultAsync(v => v.VenueId == id); 
            
            if (venue == null)
            { 
                return NotFound("Venue not found."); 
            }

            venue.VenueName = updateVenueDto.VenueName;
            venue.VenueDescription = updateVenueDto.VenueDescription;
            venue.Capacity = updateVenueDto.Capacity;
            venue.Price = updateVenueDto.Price;
            venue.Location = updateVenueDto.Location;
            venue.Latitude = updateVenueDto.Latitude;
            venue.Longitude = updateVenueDto.Longitude;
            venue.VenueStatus = updateVenueDto.VenueStatus;

            await dbContext.SaveChangesAsync(); 

            return Ok(new 
            { 
                message = "Venue updated successfully."
            });
        }

        //VenueFeatures


        [HttpGet("{venueId:guid}/features")]
        public async Task<IActionResult> GetVenueFeatures(Guid venueId)
        {
            var venueExists = await dbContext.Venues
                .AnyAsync(v => v.VenueId == venueId);

            if (!venueExists)
            {
                return NotFound("Venue not found.");
            }

            var features = await dbContext.VenueFeatures
                .Where(vf => vf.VenueId == venueId)
                .Select(vf => new VenueFeatureResponseDto
                {
                    VenueFeatureId = vf.VenueFeatureId,
                    FeatureName = vf.FeatureName,
                    FeatureDescription = vf.FeatureDescription,
                    FeatureStatus = vf.FeatureStatus,
                    VenueId = vf.VenueId
                })
                .ToListAsync();

            return Ok(features);
        }


        [HttpPost("{venueId:guid}/features")]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> CreateVenueFeature(
            Guid venueId,
            CreateVenueFeatureDto createVenueFeatureDto)
        {
            var venueExists = await dbContext.Venues
                .AnyAsync(v => v.VenueId == venueId);

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
                VenueId = venueId
            };

            dbContext.VenueFeatures.Add(venueFeature);

            await dbContext.SaveChangesAsync();

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

        [HttpPut("{venueId:guid}/features/{featureId:guid}")]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> UpdateVenueFeature(
            Guid venueId,
            Guid featureId,
            UpdateVenueFeatureDto updateVenueFeatureDto)
        {
            var venueFeature = await dbContext.VenueFeatures
                .FirstOrDefaultAsync(vf =>
                    vf.VenueFeatureId == featureId &&
                    vf.VenueId == venueId);

            if (venueFeature == null)
            {
                return NotFound("Venue feature not found.");
            }

            venueFeature.FeatureName = updateVenueFeatureDto.FeatureName;
            venueFeature.FeatureDescription = updateVenueFeatureDto.FeatureDescription;
            venueFeature.FeatureStatus = updateVenueFeatureDto.FeatureStatus;

            await dbContext.SaveChangesAsync();

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



        [HttpDelete("{venueId:guid}/features/{featureId:guid}")]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> DeleteVenueFeature(
            Guid venueId,
            Guid featureId)
        {
            var venueFeature = await dbContext.VenueFeatures
                .FirstOrDefaultAsync(vf =>
                    vf.VenueFeatureId == featureId &&
                    vf.VenueId == venueId);

            if (venueFeature == null)
            {
                return NotFound("Venue feature not found.");
            }

            dbContext.VenueFeatures.Remove(venueFeature);

            await dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Venue feature deleted successfully."
            });
        }


    }
}
