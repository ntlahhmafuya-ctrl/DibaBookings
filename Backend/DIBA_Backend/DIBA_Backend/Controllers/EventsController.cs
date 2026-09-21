using DIBA_Backend.Data;
using DIBA_Backend.Dto.Event;
using DIBA_Backend.Dto.Booking;
using DIBA_Backend.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly DIBABookingsDbContext dbContext;

        public EventsController(DIBABookingsDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // GET: api/Events
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var eventsQuery = dbContext.Events.AsQueryable();
            if (!User.IsInRole("Staff") && !User.IsInRole("Administrator"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) return Unauthorized("User ID could not be determined.");
                eventsQuery = eventsQuery.Where(e => e.UserId == userId);
            }

            var events = await eventsQuery
                .Select(e => new EventResponseDto
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDescription = e.EventDescription,
                    EventType = e.EventType,
                    EventAttendance = e.EventAttendance,
                    StartDateTime = e.StartDateTime,
                    EndDateTime = e.EndDateTime,
                    VenueId = e.VenueId,
                    UserId = e.UserId
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/Events/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetEvent(Guid id)
        {
            var eventEntity = await dbContext.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventEntity == null)
            {
                return NotFound("Event not found.");
            }

            if (!User.IsInRole("Staff") && !User.IsInRole("Administrator"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId) || eventEntity.UserId != userId) return Forbid();
            }

            var response = new EventResponseDto
            {
                EventId = eventEntity.EventId,
                EventName = eventEntity.EventName,
                EventDescription = eventEntity.EventDescription,
                EventType = eventEntity.EventType,
                EventAttendance = eventEntity.EventAttendance,
                StartDateTime = eventEntity.StartDateTime,
                EndDateTime = eventEntity.EndDateTime,
                VenueId = eventEntity.VenueId,
                UserId = eventEntity.UserId
            };

            return Ok(response);
        }

        // POST: api/Events
        [HttpPost]
        [Authorize(Roles = "Event Organiser")]
        public async Task<IActionResult> CreateEvent(
            CreateEventDto createEventDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized("User ID could not be determined.");
            }

            if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            var venueExists = await dbContext.Venues
                .AnyAsync(v => v.VenueId == createEventDto.VenueId);

            if (!venueExists)
            {
                return NotFound("Venue not found.");
            }

            if (createEventDto.EndDateTime <= createEventDto.StartDateTime)
            {
                return BadRequest("End date and time must be after the start date and time.");
            }

            var eventEntity = new Event
            {
                EventId = Guid.NewGuid(),
                EventName = createEventDto.EventName,
                EventDescription = createEventDto.EventDescription,
                EventType = createEventDto.EventType,
                EventAttendance = createEventDto.EventAttendance,
                StartDateTime = createEventDto.StartDateTime,
                EndDateTime = createEventDto.EndDateTime,
                VenueId = createEventDto.VenueId,
                UserId = userId
            };

            dbContext.Events.Add(eventEntity);
            await dbContext.SaveChangesAsync();

            var response = new EventResponseDto
            {
                EventId = eventEntity.EventId,
                EventName = eventEntity.EventName,
                EventDescription = eventEntity.EventDescription,
                EventType = eventEntity.EventType,
                EventAttendance = eventEntity.EventAttendance,
                StartDateTime = eventEntity.StartDateTime,
                EndDateTime = eventEntity.EndDateTime,
                VenueId = eventEntity.VenueId,
                UserId = eventEntity.UserId
            };

            return Ok(response);
        }

        [HttpPost("with-booking")]
        [Authorize(Roles = "Event Organiser")]
        public async Task<IActionResult> CreateEventWithBooking(CreateEventWithBookingDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) return Unauthorized("User ID could not be determined.");
            if (request.EndDateTime <= request.StartDateTime) return BadRequest("End date and time must be after the start date and time.");

            var venue = await dbContext.Venues.FirstOrDefaultAsync(v => v.VenueId == request.VenueId);
            if (venue == null) return NotFound("Venue not found.");
            if (!venue.VenueStatus.Equals("Available", StringComparison.OrdinalIgnoreCase)) return BadRequest("The selected venue is not currently available.");

            var hasConflict = await dbContext.Bookings.AnyAsync(b =>
                b.VenueId == request.VenueId &&
                b.StartDateTime < request.EndDateTime &&
                b.EndDateTime > request.StartDateTime &&
                b.BookingStatus != null &&
                (b.BookingStatus.StatusName == "Pending" || b.BookingStatus.StatusName == "Approved"));
            if (hasConflict) return Conflict("The selected venue is already booked or awaiting approval for the requested time.");

            var pendingStatus = await dbContext.BookingStatuses.FirstOrDefaultAsync(bs => bs.StatusName == "Pending");
            if (pendingStatus == null) return StatusCode(500, "Pending booking status could not be found.");

            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                var eventEntity = new Event
                {
                    EventId = Guid.NewGuid(),
                    EventName = request.EventName,
                    EventDescription = request.EventDescription,
                    EventType = request.EventType,
                    EventAttendance = request.EventAttendance,
                    StartDateTime = request.StartDateTime,
                    EndDateTime = request.EndDateTime,
                    VenueId = request.VenueId,
                    UserId = userId
                };
                var booking = new Models.Entities.Booking
                {
                    BookingId = Guid.NewGuid(),
                    BookingDate = DateTime.UtcNow,
                    StartDateTime = request.StartDateTime,
                    EndDateTime = request.EndDateTime,
                    SpecialRequirements = request.SpecialRequirements,
                    UserId = userId,
                    EventId = eventEntity.EventId,
                    VenueId = request.VenueId,
                    BookingStatusId = pendingStatus.BookingStatusId
                };
                dbContext.Events.Add(eventEntity);
                dbContext.Bookings.Add(booking);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    Event = new EventResponseDto
                    {
                        EventId = eventEntity.EventId,
                        EventName = eventEntity.EventName,
                        EventDescription = eventEntity.EventDescription,
                        EventType = eventEntity.EventType,
                        EventAttendance = eventEntity.EventAttendance,
                        StartDateTime = eventEntity.StartDateTime,
                        EndDateTime = eventEntity.EndDateTime,
                        VenueId = eventEntity.VenueId,
                        UserId = eventEntity.UserId
                    },
                    Booking = new BookingResponseDto
                    {
                        BookingId = booking.BookingId,
                        BookingDate = booking.BookingDate,
                        StartDateTime = booking.StartDateTime,
                        EndDateTime = booking.EndDateTime,
                        SpecialRequirements = booking.SpecialRequirements,
                        UserId = booking.UserId,
                        EventId = booking.EventId,
                        VenueId = booking.VenueId,
                        BookingStatusId = booking.BookingStatusId,
                        StatusName = pendingStatus.StatusName,
                        EventName = eventEntity.EventName,
                        VenueName = venue.VenueName
                    }
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // PUT: api/Events/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Event Organiser")]
        public async Task<IActionResult> UpdateEvent(
            Guid id,
            UpdateEventDto updateEventDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized("User ID could not be determined.");
            }

            if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            var eventEntity = await dbContext.Events
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventEntity == null)
            {
                return NotFound("Event not found.");
            }

            if (eventEntity.UserId != userId)
            {
                return Forbid();
            }

            var venueExists = await dbContext.Venues
                .AnyAsync(v => v.VenueId == updateEventDto.VenueId);

            if (!venueExists)
            {
                return NotFound("Venue not found.");
            }

            if (updateEventDto.EndDateTime <= updateEventDto.StartDateTime)
            {
                return BadRequest("End date and time must be after the start date and time.");
            }

            eventEntity.EventName = updateEventDto.EventName;
            eventEntity.EventDescription = updateEventDto.EventDescription;
            eventEntity.EventType = updateEventDto.EventType;
            eventEntity.EventAttendance = updateEventDto.EventAttendance;
            eventEntity.StartDateTime = updateEventDto.StartDateTime;
            eventEntity.EndDateTime = updateEventDto.EndDateTime;
            eventEntity.VenueId = updateEventDto.VenueId;

            await dbContext.SaveChangesAsync();

            var response = new EventResponseDto
            {
                EventId = eventEntity.EventId,
                EventName = eventEntity.EventName,
                EventDescription = eventEntity.EventDescription,
                EventType = eventEntity.EventType,
                EventAttendance = eventEntity.EventAttendance,
                StartDateTime = eventEntity.StartDateTime,
                EndDateTime = eventEntity.EndDateTime,
                VenueId = eventEntity.VenueId,
                UserId = eventEntity.UserId
            };

            return Ok(response);
        }
    }
}
