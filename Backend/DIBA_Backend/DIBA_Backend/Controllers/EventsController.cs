using DIBA_Backend.Data;
using DIBA_Backend.Dto.Booking;
using DIBA_Backend.Dto.Event;
using DIBA_Backend.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
    // Similar logic: requiring authentication before accessing protected
    // controller actions.
    // DIBA adaptation: all event operations require an authenticated user.
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly DIBABookingsDbContext _dbContext;

        public EventsController(DIBABookingsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/Events
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            IQueryable<Event> eventsQuery = _dbContext.Events;

            // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
            // Similar logic: using ClaimsPrincipal.IsInRole to determine
            // what data or operations a user is allowed to access.
            // DIBA adaptation: Staff and Administrators can view all events,
            // while Event Organisers are restricted to their own events.
            if (!User.IsInRole("Staff") && !User.IsInRole("Administrator"))
            {
                // Reference: Microsoft Learn, "Claim-based authorization
                // in ASP.NET Core".
                // Similar logic: obtaining information about the authenticated
                // user from a claim.
                // DIBA adaptation: the NameIdentifier claim stores the
                // UserId used by the DIBA database.
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized("User ID could not be determined.");
                }

                if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    return Unauthorized("Invalid user ID.");
                }

                // DIBA-specific access-control rule:
                // Event Organisers can only retrieve events that belong
                // to their own UserId.
                eventsQuery = eventsQuery
                    .Where(eventEntity => eventEntity.UserId == userId);
            }

            // Similar logic: projecting database entities into a response
            // object rather than returning the entity directly.
            // DIBA adaptation: EventResponseDto defines the event data
            // exposed to the frontend.
            var events = await eventsQuery
                .Select(eventEntity => new EventResponseDto
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
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/Events/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetEvent(Guid id)
        {
            var eventEntity = await _dbContext.Events
                .FirstOrDefaultAsync(
                    eventEntity => eventEntity.EventId == id);

            if (eventEntity == null)
            {
                return NotFound("Event not found.");
            }

            // Reference: Microsoft Learn, "Role-based authorization in
            // ASP.NET Core".
            // Similar logic: checking the authenticated user's role before
            // allowing access to protected information.
            // DIBA adaptation: Staff and Administrators can view any event,
            // while Event Organisers are restricted to their own events.
            if (!User.IsInRole("Staff") && !User.IsInRole("Administrator"))
            {
                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(
                        "User ID could not be determined.");
                }

                if (!Guid.TryParse(
                        userIdClaim.Value,
                        out Guid userId))
                {
                    return Unauthorized("Invalid user ID.");
                }

                // DIBA-specific ownership check:
                // prevents an Event Organiser from accessing another
                // organiser's event by changing the event ID.
                if (eventEntity.UserId != userId)
                {
                    return Forbid();
                }
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
            // Reference: Microsoft Learn, "Claim-based authorization
            // in ASP.NET Core".
            // Similar logic: obtaining the identity of the current user
            // from an authentication claim.
            // DIBA adaptation: the UserId identifies the organiser who
            // created the event.
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized(
                    "User ID could not be determined.");
            }

            if (!Guid.TryParse(
                    userIdClaim.Value,
                    out Guid userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            // Reference: ErmaoCyber, "meeting-room-reservation-api".
            // Similar logic: validating reservation start and end times
            // before allowing a booking-related operation.
            // DIBA adaptation: an event cannot end at or before its
            // start time.
            if (createEventDto.EndDateTime <=
                createEventDto.StartDateTime)
            {
                return BadRequest(
                    "End date and time must be after the start date and time.");
            }

            // DIBA-specific venue validation.
            // The event cannot be created if the selected venue does
            // not exist.
            var venue = await _dbContext.Venues
                .FirstOrDefaultAsync(
                    venue => venue.VenueId == createEventDto.VenueId);

            if (venue == null)
            {
                return NotFound("Venue not found.");
            }

            // DIBA-specific business rule:
            // events may only be created using an available venue.
            if (!venue.VenueStatus.Equals(
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "The selected venue is not currently available.");
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

            _dbContext.Events.Add(eventEntity);

            await _dbContext.SaveChangesAsync();

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

        // POST: api/Events/with-booking
        // Creates the event and its booking together.
        [HttpPost("with-booking")]
        [Authorize(Roles = "Event Organiser")]
        public async Task<IActionResult> CreateEventWithBooking(
            CreateEventWithBookingDto request)
        {
            // Reference: Microsoft Learn, "Claim-based authorization
            // in ASP.NET Core".
            // Similar logic: retrieving the authenticated user's identity
            // from a claim.
            // DIBA adaptation: the organiser's UserId is assigned to both
            // the event and booking.
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized(
                    "User ID could not be determined.");
            }

            if (!Guid.TryParse(
                    userIdClaim.Value,
                    out Guid userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            if (!request.AcknowledgementAccepted)
            {
                return BadRequest(
                    "You must acknowledge the booking requirements before submitting the booking.");
            }

            // Reference: ErmaoCyber, "meeting-room-reservation-api".
            // Similar logic: validating that the requested time period
            // is logically valid before checking availability.
            if (request.EndDateTime <= request.StartDateTime)
            {
                return BadRequest(
                    "End date and time must be after the start date and time.");
            }

            // DIBA-specific venue existence validation.
            var venue = await _dbContext.Venues
                .FirstOrDefaultAsync(
                    venue => venue.VenueId == request.VenueId);

            if (venue == null)
            {
                return NotFound("Venue not found.");
            }

            // DIBA-specific venue availability rule.
            if (!venue.VenueStatus.Equals(
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "The selected venue is not currently available.");
            }

            // Reference: ErmaoCyber, "meeting-room-reservation-api".
            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: checking whether another reservation overlaps
            // the requested reservation period and preventing conflicting
            // bookings.
            //
            // DIBA adaptation: only Pending and Approved bookings are
            // considered active conflicts.
            var hasConflict =
                await _dbContext.Bookings.AnyAsync(booking =>
                    booking.VenueId == request.VenueId &&
                    booking.StartDateTime < request.EndDateTime &&
                    booking.EndDateTime > request.StartDateTime &&
                    booking.BookingStatus != null &&
                    (booking.BookingStatus.StatusName == "Pending" ||
                     booking.BookingStatus.StatusName == "Approved"));

            if (hasConflict)
            {
                return Conflict(
                    "The selected venue is already booked or awaiting approval for the requested time.");
            }

            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: bookings have explicit states/statuses that
            // control the booking workflow.
            // DIBA adaptation: every newly created booking starts with
            // the Pending status.
            var pendingStatus =
                await _dbContext.BookingStatuses
                    .FirstOrDefaultAsync(
                        bookingStatus =>
                            bookingStatus.StatusName == "Pending");

            if (pendingStatus == null)
            {
                return StatusCode(
                    500,
                    "Pending booking status could not be found.");
            }

            // Similar transaction logic: treating the creation of related
            // database records as one unit of work.
            //
            // DIBA adaptation: an Event and its Booking are created
            // together. If one part fails, the transaction is rolled back
            // so that the system does not keep an incomplete event/booking.
            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync();

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

                var booking = new Booking
                {
                    BookingId = Guid.NewGuid(),
                    BookingDate = DateTime.UtcNow,
                    StartDateTime = request.StartDateTime,
                    EndDateTime = request.EndDateTime,
                    SpecialRequirements = request.SpecialRequirements,

                    AcknowledgementAccepted =
                        request.AcknowledgementAccepted,

                    AcknowledgementAcceptedAt =
                        DateTime.UtcNow,

                    UserId = userId,
                    EventId = eventEntity.EventId,
                    VenueId = request.VenueId,
                    BookingStatusId =
                        pendingStatus.BookingStatusId
                };

                _dbContext.Events.Add(eventEntity);
                _dbContext.Bookings.Add(booking);

                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    Event = new EventResponseDto
                    {
                        EventId = eventEntity.EventId,
                        EventName = eventEntity.EventName,
                        EventDescription =
                            eventEntity.EventDescription,
                        EventType = eventEntity.EventType,
                        EventAttendance =
                            eventEntity.EventAttendance,
                        StartDateTime =
                            eventEntity.StartDateTime,
                        EndDateTime =
                            eventEntity.EndDateTime,
                        VenueId = eventEntity.VenueId,
                        UserId = eventEntity.UserId
                    },

                    Booking = new BookingResponseDto
                    {
                        BookingId = booking.BookingId,
                        BookingDate = booking.BookingDate,
                        StartDateTime = booking.StartDateTime,
                        EndDateTime = booking.EndDateTime,
                        SpecialRequirements =
        booking.SpecialRequirements,

                        AcknowledgementAccepted =
        booking.AcknowledgementAccepted,

                        AcknowledgementAcceptedAt =
        booking.AcknowledgementAcceptedAt,

                        UserId = booking.UserId,
                        StatusName =
                            pendingStatus.StatusName,
                        EventName =
                            eventEntity.EventName,
                        VenueName =
                            venue.VenueName
                    }
                });
            }
            catch
            {
                // DIBA-specific transaction recovery:
                // if either the event or booking cannot be saved,
                // undo the database changes made during this operation.
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
            // Reference: Microsoft Learn, "Claim-based authorization
            // in ASP.NET Core".
            // Similar logic: identifying the authenticated user through
            // a claim.
            // DIBA adaptation: used to determine which organiser owns
            // the event being updated.
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized(
                    "User ID could not be determined.");
            }

            if (!Guid.TryParse(
                    userIdClaim.Value,
                    out Guid userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            var eventEntity = await _dbContext.Events
                .FirstOrDefaultAsync(
                    eventEntity => eventEntity.EventId == id);

            if (eventEntity == null)
            {
                return NotFound("Event not found.");
            }

            // DIBA-specific ownership rule:
            // only the organiser who created the event can modify it.
            if (eventEntity.UserId != userId)
            {
                return Forbid();
            }

            // Reference: ErmaoCyber, "meeting-room-reservation-api".
            // Similar logic: validating the requested reservation period.
            if (updateEventDto.EndDateTime <=
                updateEventDto.StartDateTime)
            {
                return BadRequest(
                    "End date and time must be after the start date and time.");
            }

            var venue = await _dbContext.Venues
                .FirstOrDefaultAsync(
                    venue => venue.VenueId ==
                             updateEventDto.VenueId);

            if (venue == null)
            {
                return NotFound("Venue not found.");
            }

            // DIBA-specific venue availability rule.
            if (!venue.VenueStatus.Equals(
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "The selected venue is not currently available.");
            }

            // Reference: ErmaoCyber, "meeting-room-reservation-api".
            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: checking for another booking whose time
            // overlaps the requested time.
            //
            // DIBA adaptation: the current event's own booking is excluded
            // using EventId != eventEntity.EventId.
            var hasConflict =
                await _dbContext.Bookings.AnyAsync(booking =>
                    booking.VenueId == updateEventDto.VenueId &&
                    booking.StartDateTime <
                        updateEventDto.EndDateTime &&
                    booking.EndDateTime >
                        updateEventDto.StartDateTime &&
                    booking.EventId != eventEntity.EventId &&
                    booking.BookingStatus != null &&
                    (booking.BookingStatus.StatusName == "Pending" ||
                     booking.BookingStatus.StatusName == "Approved"));

            if (hasConflict)
            {
                return Conflict(
                    "The selected venue is already booked or awaiting approval for the requested time.");
            }

            eventEntity.EventName =
                updateEventDto.EventName;

            eventEntity.EventDescription =
                updateEventDto.EventDescription;

            eventEntity.EventType =
                updateEventDto.EventType;

            eventEntity.EventAttendance =
                updateEventDto.EventAttendance;

            eventEntity.StartDateTime =
                updateEventDto.StartDateTime;

            eventEntity.EndDateTime =
                updateEventDto.EndDateTime;

            eventEntity.VenueId =
                updateEventDto.VenueId;

            await _dbContext.SaveChangesAsync();

            var response = new EventResponseDto
            {
                EventId = eventEntity.EventId,
                EventName = eventEntity.EventName,
                EventDescription =
                    eventEntity.EventDescription,
                EventType = eventEntity.EventType,
                EventAttendance =
                    eventEntity.EventAttendance,
                StartDateTime =
                    eventEntity.StartDateTime,
                EndDateTime =
                    eventEntity.EndDateTime,
                VenueId = eventEntity.VenueId,
                UserId = eventEntity.UserId
            };

            return Ok(response);
        }
    }
}
