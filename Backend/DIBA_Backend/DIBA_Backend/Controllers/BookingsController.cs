using DIBA_Backend.Data;
using DIBA_Backend.Dto.Booking;
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
    // Similar logic: restricting access to authenticated users.
    // DIBA adaptation: the entire BookingsController requires the user
    // to be authenticated before accessing booking information.
    // https://learn.microsoft.com/aspnet/core/security/authorization/roles
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly DIBABookingsDbContext _dbContext;

        public BookingsController(DIBABookingsDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        // GET: api/Bookings
        [HttpGet]
        public async Task<IActionResult> GetBookings()
        {
            IQueryable<Booking> bookingsQuery = _dbContext.Bookings;


            // Reference: Microsoft Learn, "Role-based authorization in
            // ASP.NET Core".
            // Similar logic: checking the authenticated user's role before
            // allowing access to information or operations.
            // DIBA adaptation: Staff and Administrators can see all bookings,
            // while Event Organisers are restricted to their own bookings.
            // https://learn.microsoft.com/aspnet/core/security/authorization/roles
            if (!User.IsInRole("Staff") &&
                !User.IsInRole("Administrator"))
            {
                // Reference: Microsoft Learn claims documentation.
                // Similar logic: retrieving information about the current
                // authenticated user from a claim.
                // DIBA adaptation: NameIdentifier contains the DIBA UserId.
                // https://learn.microsoft.com/aspnet/core/security/authentication/claims
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


                // DIBA-specific data-access rule:
                // Event Organisers can only retrieve bookings belonging
                // to their authenticated user account.
                bookingsQuery = bookingsQuery
                    .Where(booking =>
                        booking.UserId == userId);
            }


            // Similar EF Core projection pattern to the reviewed
            // booking/authentication projects.
            // DIBA adaptation: related User, Event, Venue and BookingStatus
            // information is projected into a BookingResponseDto instead
            // of exposing the complete database entity.
            var bookings = await bookingsQuery
                .Include(booking => booking.User)
                .Include(booking => booking.Event)
                .Include(booking => booking.Venue)
                .Include(booking => booking.BookingStatus)
                .Select(booking => new BookingResponseDto
                {
                    BookingId = booking.BookingId,
                    BookingDate = booking.BookingDate,
                    StartDateTime = booking.StartDateTime,
                    EndDateTime = booking.EndDateTime,
                    SpecialRequirements = booking.SpecialRequirements,
                    AdminNotes = booking.AdminNotes,
                    UserId = booking.UserId,
                    EventId = booking.EventId,
                    VenueId = booking.VenueId,
                    BookingStatusId = booking.BookingStatusId,

                    StatusName =
                        booking.BookingStatus != null
                            ? booking.BookingStatus.StatusName
                            : string.Empty,

                    OrganiserName =
                        booking.User != null
                            ? booking.User.FirstName +
                              " " +
                              booking.User.LastName
                            : string.Empty,

                    EventName =
                        booking.Event != null
                            ? booking.Event.EventName
                            : string.Empty,

                    VenueName =
                        booking.Venue != null
                            ? booking.Venue.VenueName
                            : string.Empty
                })
                .ToListAsync();

            return Ok(bookings);
        }


        // GET: api/Bookings/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetBooking(Guid id)
        {
            var booking = await _dbContext.Bookings
                .Include(booking => booking.User)
                .Include(booking => booking.Event)
                .Include(booking => booking.Venue)
                .Include(booking => booking.BookingStatus)
                .FirstOrDefaultAsync(
                    booking => booking.BookingId == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }


            // Reference: Microsoft Learn, "Role-based authorization in
            // ASP.NET Core".
            // Similar logic: using roles to determine whether a user can
            // access another user's data.
            // DIBA adaptation: Staff and Administrators can view any booking,
            // while Event Organisers must own the booking.
            if (!User.IsInRole("Staff") &&
                !User.IsInRole("Administrator"))
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

                // DIBA-specific ownership check.
                if (booking.UserId != userId)
                {
                    return Forbid();
                }
            }


            // DIBA adaptation:
            // The database entity is converted to a response DTO so that
            // only the information required by the frontend is returned.
            var response = new BookingResponseDto
            {
                BookingId = booking.BookingId,
                BookingDate = booking.BookingDate,
                StartDateTime = booking.StartDateTime,
                EndDateTime = booking.EndDateTime,
                SpecialRequirements = booking.SpecialRequirements,
                AdminNotes = booking.AdminNotes,
                UserId = booking.UserId,
                EventId = booking.EventId,
                VenueId = booking.VenueId,
                BookingStatusId = booking.BookingStatusId,

                StatusName =
                    booking.BookingStatus?.StatusName ??
                    string.Empty,

                OrganiserName =
                    booking.User == null
                        ? string.Empty
                        : booking.User.FirstName +
                          " " +
                          booking.User.LastName,

                EventName =
                    booking.Event?.EventName ??
                    string.Empty,

                VenueName =
                    booking.Venue?.VenueName ??
                    string.Empty
            };

            return Ok(response);
        }


        // GET: api/Bookings/availability
        [HttpGet("availability")]
        public async Task<IActionResult> CheckAvailability(
            Guid venueId,
            DateTime startDateTime,
            DateTime endDateTime)
        {

            // Reference: ErmaoCyber, "Meeting Room Reservation API".
            // Similar logic: validating that the requested time range is
            // logically valid before checking room availability.
            // The referenced project specifically identifies the rule that
            // the end time must be after the start time.
            // DIBA adaptation: the same validation is applied to venue
            // bookings.
            // https://github.com/ErmaoCyber/meeting-room-reservation-api
            if (endDateTime <= startDateTime)
            {
                return BadRequest(
                    "End date and time must be after the start date and time.");
            }


            // DIBA-specific existence check.
            var venue = await _dbContext.Venues
                .FirstOrDefaultAsync(
                    venue => venue.VenueId == venueId);

            if (venue == null)
            {
                return NotFound("Venue not found.");
            }


            // DIBA-specific venue availability rule.
            // A venue must itself be marked Available before its booking
            // schedule is considered.
            if (!venue.VenueStatus.Equals(
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    available = false,
                    reason =
                        "The selected venue is not currently available."
                });
            }


            // Reference: ErmaoCyber, "Meeting Room Reservation API".
            // Similar logic: checking whether a requested time range overlaps
            // with an existing room reservation.
            //
            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: conference booking includes conflict checking
            // and booking status management.
            //
            // DIBA adaptation:
            // - room becomes venue;
            // - reservations become bookings;
            // - only Pending and Approved bookings block the requested slot.
            //
            // https://github.com/ErmaoCyber/meeting-room-reservation-api
            // https://github.com/JedAngelo/ConferenceBookingApi
            var conflict = await _dbContext.Bookings
                .AnyAsync(booking =>
                    booking.VenueId == venueId &&

                    // Existing booking starts before requested booking ends.
                    booking.StartDateTime < endDateTime &&

                    // Existing booking ends after requested booking starts.
                    booking.EndDateTime > startDateTime &&

                    booking.BookingStatus != null &&

                    (
                        booking.BookingStatus.StatusName == "Pending" ||
                        booking.BookingStatus.StatusName == "Approved"
                    ));

            return Ok(new
            {
                available = !conflict,

                reason = conflict
                    ? "The selected venue is already booked or awaiting approval for the requested time."
                    : null
            });
        }


        // POST: api/Bookings
        [HttpPost]

        // Reference: Microsoft Learn, "Role-based authorization in
        // ASP.NET Core".
        // Similar logic: restricting a controller action to users
        // belonging to a specific role.
        // DIBA adaptation: only Event Organisers may create bookings.
        // https://learn.microsoft.com/aspnet/core/security/authorization/roles
        [Authorize(Roles = "Event Organiser")]
        public async Task<IActionResult> CreateBooking(
            CreateBookingDto createBookingDto)
        {

            // Reference: Microsoft Learn claims documentation.
            // Similar logic: obtaining the current user's identity from
            // a claim.
            // DIBA adaptation: the UserId is used to associate the new
            // booking with the authenticated Event Organiser.
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


            // Reference: ErmaoCyber, "Meeting Room Reservation API".
            // Similar logic: the requested end time must be after the
            // requested start time.
            // DIBA adaptation: the rule is applied to venue bookings.
            if (createBookingDto.EndDateTime <=
                createBookingDto.StartDateTime)
            {
                return BadRequest(
                    "End date and time must be after the start date and time.");
            }


            // DIBA-specific existence validation.
            var eventEntity = await _dbContext.Events
                .FirstOrDefaultAsync(
                    eventEntity =>
                        eventEntity.EventId ==
                        createBookingDto.EventId);

            if (eventEntity == null)
            {
                return NotFound("Event not found.");
            }


            // DIBA-specific existence validation.
            var venue = await _dbContext.Venues
                .FirstOrDefaultAsync(
                    venue =>
                        venue.VenueId ==
                        createBookingDto.VenueId);

            if (venue == null)
            {
                return NotFound("Venue not found.");
            }


            // DIBA-specific business rule.
            if (!venue.VenueStatus.Equals(
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "The selected venue is not currently available.");
            }


            // DIBA-specific ownership rule:
            // an Event Organiser may only create a booking for an event
            // that belongs to that organiser.
            if (eventEntity.UserId != userId)
            {
                return Forbid();
            }


            // Reference: ErmaoCyber, "Meeting Room Reservation API".
            // Similar logic: preventing double-booking by checking whether
            // another reservation overlaps the requested time interval.
            //
            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: conference-room bookings include conflict
            // checking before a booking is accepted.
            //
            // DIBA adaptation:
            // the check uses VenueId, StartDateTime and EndDateTime and
            // considers Pending and Approved bookings as conflicts.
            var hasConflict = await _dbContext.Bookings
                .AnyAsync(booking =>
                    booking.VenueId ==
                    createBookingDto.VenueId &&

                    booking.StartDateTime <
                    createBookingDto.EndDateTime &&

                    booking.EndDateTime >
                    createBookingDto.StartDateTime &&

                    booking.BookingStatus != null &&

                    (
                        booking.BookingStatus.StatusName == "Pending" ||
                        booking.BookingStatus.StatusName == "Approved"
                    ));

            if (hasConflict)
            {
                return Conflict(
                    "The selected venue is already booked or awaiting approval for the requested time.");
            }


            // DIBA-specific status workflow.
            // A newly created booking begins in the Pending state.
            var pendingStatus = await _dbContext.BookingStatuses
                .FirstOrDefaultAsync(
                    bookingStatus =>
                        bookingStatus.StatusName ==
                        "Pending");

            if (pendingStatus == null)
            {
                return StatusCode(
                    500,
                    "Pending booking status could not be found.");
            }


            var booking = new Booking
            {
                BookingId = Guid.NewGuid(),
                BookingDate = DateTime.UtcNow,
                StartDateTime =
                    createBookingDto.StartDateTime,
                EndDateTime =
                    createBookingDto.EndDateTime,
                SpecialRequirements =
                    createBookingDto.SpecialRequirements,
                AdminNotes = null,
                UserId = userId,
                EventId = createBookingDto.EventId,
                VenueId = createBookingDto.VenueId,
                BookingStatusId =
                    pendingStatus.BookingStatusId
            };

            _dbContext.Bookings.Add(booking);

            await _dbContext.SaveChangesAsync();


            // DIBA adaptation:
            // return a DTO rather than directly returning the database entity.
            var response = new BookingResponseDto
            {
                BookingId = booking.BookingId,
                BookingDate = booking.BookingDate,
                StartDateTime = booking.StartDateTime,
                EndDateTime = booking.EndDateTime,
                SpecialRequirements =
                    booking.SpecialRequirements,
                AdminNotes = booking.AdminNotes,
                UserId = booking.UserId,
                EventId = booking.EventId,
                VenueId = booking.VenueId,
                BookingStatusId =
                    booking.BookingStatusId
            };

            return Ok(response);
        }


        // PUT: api/Bookings/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Event Organiser")]
        public async Task<IActionResult> UpdateBooking(
            Guid id,
            UpdateBookingDto updateBookingDto)
        {

            // Similar claims-based user identification pattern used
            // throughout ASP.NET Core authenticated applications.
            // DIBA adaptation: the NameIdentifier claim identifies the
            // organiser performing the update.
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


            var booking = await _dbContext.Bookings
                .FirstOrDefaultAsync(
                    booking =>
                        booking.BookingId == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }


            // DIBA-specific ownership check.
            // The authenticated organiser must own the booking being edited.
            if (booking.UserId != userId)
            {
                return Forbid();
            }


            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: bookings use explicit status values such as
            // Pending, Approved and Rejected to control booking operations.
            // DIBA adaptation: only Pending bookings may be edited.
            var currentStatus = await _dbContext.BookingStatuses
                .FirstOrDefaultAsync(
                    bookingStatus =>
                        bookingStatus.BookingStatusId ==
                        booking.BookingStatusId);

            if (currentStatus == null)
            {
                return StatusCode(
                    500,
                    "Booking status could not be found.");
            }

            if (!currentStatus.StatusName.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Only pending bookings can be updated.");
            }


            // Reference: ErmaoCyber, "Meeting Room Reservation API".
            // Similar time-range validation.
            if (updateBookingDto.EndDateTime <=
                updateBookingDto.StartDateTime)
            {
                return BadRequest(
                    "End date and time must be after the start date and time.");
            }


            // DIBA-specific event ownership validation.
            var eventEntity = await _dbContext.Events
                .FirstOrDefaultAsync(
                    eventEntity =>
                        eventEntity.EventId ==
                        updateBookingDto.EventId);

            if (eventEntity == null)
            {
                return NotFound("Event not found.");
            }

            if (eventEntity.UserId != userId)
            {
                return Forbid();
            }


            var venue = await _dbContext.Venues
                .FirstOrDefaultAsync(
                    venue =>
                        venue.VenueId ==
                        updateBookingDto.VenueId);

            if (venue == null)
            {
                return NotFound("Venue not found.");
            }

            if (!venue.VenueStatus.Equals(
                    "Available",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "The selected venue is not currently available.");
            }


            // Reference: ErmaoCyber, "Meeting Room Reservation API".
            // Similar logic: prevent overlapping reservations when a booking
            // is created or changed.
            //
            // DIBA adaptation: the current booking is excluded using
            // `booking.BookingId != id`. This is necessary because the
            // booking being edited should not conflict with itself.
            //
            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar domain logic: booking updates are subject to
            // conflict checking.
            var hasConflict = await _dbContext.Bookings
                .AnyAsync(booking =>
                    booking.BookingId != id &&

                    booking.VenueId ==
                    updateBookingDto.VenueId &&

                    booking.StartDateTime <
                    updateBookingDto.EndDateTime &&

                    booking.EndDateTime >
                    updateBookingDto.StartDateTime &&

                    booking.BookingStatus != null &&

                    (
                        booking.BookingStatus.StatusName == "Pending" ||
                        booking.BookingStatus.StatusName == "Approved"
                    ));

            if (hasConflict)
            {
                return Conflict(
                    "The selected venue is already booked or awaiting approval for the requested time.");
            }


            booking.EventId =
                updateBookingDto.EventId;

            booking.VenueId =
                updateBookingDto.VenueId;

            booking.StartDateTime =
                updateBookingDto.StartDateTime;

            booking.EndDateTime =
                updateBookingDto.EndDateTime;

            booking.SpecialRequirements =
                updateBookingDto.SpecialRequirements;

            await _dbContext.SaveChangesAsync();


            var response = new BookingResponseDto
            {
                BookingId = booking.BookingId,
                BookingDate = booking.BookingDate,
                StartDateTime = booking.StartDateTime,
                EndDateTime = booking.EndDateTime,
                SpecialRequirements =
                    booking.SpecialRequirements,
                AdminNotes = booking.AdminNotes,
                UserId = booking.UserId,
                EventId = booking.EventId,
                VenueId = booking.VenueId,
                BookingStatusId =
                    booking.BookingStatusId
            };

            return Ok(response);
        }


        // PUT: api/Bookings/{id}/approve
        [HttpPut("{id:guid}/approve")]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> ApproveBooking(Guid id)
        {
            // Similar claims-based identification pattern:
            // the authenticated staff member/administrator is identified
            // from the NameIdentifier claim.
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


            var booking = await _dbContext.Bookings
                .FirstOrDefaultAsync(
                    booking =>
                        booking.BookingId == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }


            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: bookings have explicit status values and
            // status changes form part of the booking workflow.
            // DIBA adaptation: an Approved status is retrieved before
            // changing the booking.
            var approvedStatus = await _dbContext.BookingStatuses
                .FirstOrDefaultAsync(
                    bookingStatus =>
                        bookingStatus.StatusName ==
                        "Approved");

            if (approvedStatus == null)
            {
                return StatusCode(
                    500,
                    "Approved booking status could not be found.");
            }


            var currentStatus = await _dbContext.BookingStatuses
                .FirstOrDefaultAsync(
                    bookingStatus =>
                        bookingStatus.BookingStatusId ==
                        booking.BookingStatusId);

            if (currentStatus == null)
            {
                return StatusCode(
                    500,
                    "Current booking status could not be found.");
            }


            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: booking status controls what operations may
            // be performed on a booking.
            // DIBA adaptation: only Pending bookings may be approved.
            if (!currentStatus.StatusName.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Only pending bookings can be approved.");
            }


            // Change the booking status.
            booking.BookingStatusId =
                approvedStatus.BookingStatusId;


            // Reference: practical audit/event logging pattern.
            // DIBA adaptation: an approval is considered an important
            // system action, so the user performing it and the action
            // timestamp are recorded.
            var auditLog = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = "Booking Approved",
                LogDescription =
                    $"Booking {booking.BookingId} was approved.",
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };

            _dbContext.AuditLogs.Add(auditLog);


            // DIBA-specific notification workflow.
            // When a booking is approved, a notification is created for
            // the Event Organiser who owns the booking.
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                NotificationType = "Booking Approved",
                Message =
                    "Your venue booking has been approved.",
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                UserId = booking.UserId,
                BookingId = booking.BookingId
            };

            _dbContext.Notifications.Add(notification);

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Booking approved successfully.",
                bookingId =
                    booking.BookingId,
                status =
                    approvedStatus.StatusName
            });
        }


        // PUT: api/Bookings/{id}/reject
        [HttpPut("{id:guid}/reject")]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> RejectBooking(
            Guid id,
            RejectBookingDto rejectBookingDto)
        {

            // Same claims-based user identification pattern used
            // for the approval operation.
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


            var booking = await _dbContext.Bookings
                .FirstOrDefaultAsync(
                    booking =>
                        booking.BookingId == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }


            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: explicit rejected booking status.
            var rejectedStatus = await _dbContext.BookingStatuses
                .FirstOrDefaultAsync(
                    bookingStatus =>
                        bookingStatus.StatusName ==
                        "Rejected");

            if (rejectedStatus == null)
            {
                return StatusCode(
                    500,
                    "Rejected booking status could not be found.");
            }


            var currentStatus = await _dbContext.BookingStatuses
                .FirstOrDefaultAsync(
                    bookingStatus =>
                        bookingStatus.BookingStatusId ==
                        booking.BookingStatusId);

            if (currentStatus == null)
            {
                return StatusCode(
                    500,
                    "Current booking status could not be found.");
            }


            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar status-driven booking workflow.
            // DIBA adaptation: only Pending bookings can be rejected.
            if (!currentStatus.StatusName.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Only pending bookings can be rejected.");
            }


            // DIBA-specific business rule:
            // a rejection must contain a reason so that the organiser
            // understands why the request was rejected.
            if (string.IsNullOrWhiteSpace(
                    rejectBookingDto.Reason))
            {
                return BadRequest(
                    "A rejection reason is required.");
            }


            // Store the rejection reason and update the status.
            booking.AdminNotes =
                rejectBookingDto.Reason;

            booking.BookingStatusId =
                rejectedStatus.BookingStatusId;


            // DIBA audit logging:
            // rejection is recorded together with the reason and the
            // authenticated staff member/administrator who performed it.
            var auditLog = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = "Booking Rejected",
                LogDescription =
                    $"Booking {booking.BookingId} was rejected. " +
                    $"Reason: {rejectBookingDto.Reason}",
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };

            _dbContext.AuditLogs.Add(auditLog);


            // DIBA notification workflow:
            // the organiser receives the rejection reason as part of
            // the notification.
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                NotificationType = "Booking Rejected",
                Message =
                    $"Your venue booking has been rejected. " +
                    $"Reason: {rejectBookingDto.Reason}",
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                UserId = booking.UserId,
                BookingId = booking.BookingId
            };

            _dbContext.Notifications.Add(notification);

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Booking rejected successfully.",
                bookingId =
                    booking.BookingId,
                status =
                    rejectedStatus.StatusName,
                reason =
                    booking.AdminNotes
            });
        }


        // PUT: api/Bookings/{id}/cancel
        [HttpPut("{id:guid}/cancel")]
        public async Task<IActionResult> CancelBooking(Guid id)
        {

            // Same claims-based identity retrieval used by the other
            // booking operations.
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


            var booking = await _dbContext.Bookings
                .FirstOrDefaultAsync(
                    booking =>
                        booking.BookingId == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }


            // Reference: Microsoft Learn, "Role-based authorization
            // in ASP.NET Core".
            // Similar logic: checking whether an authenticated user
            // belongs to one of the permitted roles.
            // DIBA adaptation: Staff and Administrators receive broader
            // cancellation permissions.
            var isStaffOrAdministrator =
                User.IsInRole("Staff") ||
                User.IsInRole("Administrator");


            // DIBA-specific ownership and role rule:
            // Event Organisers may cancel their own bookings, while
            // Staff and Administrators may cancel any booking.
            if (booking.UserId != userId &&
                !isStaffOrAdministrator)
            {
                return Forbid();
            }


            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: booking status is used to control the
            // allowed operations on a reservation.
            var cancelledStatus = await _dbContext.BookingStatuses
                .FirstOrDefaultAsync(
                    bookingStatus =>
                        bookingStatus.StatusName ==
                        "Cancelled");

            if (cancelledStatus == null)
            {
                return StatusCode(
                    500,
                    "Cancelled booking status could not be found.");
            }


            var currentStatus = await _dbContext.BookingStatuses
                .FirstOrDefaultAsync(
                    bookingStatus =>
                        bookingStatus.BookingStatusId ==
                        booking.BookingStatusId);

            if (currentStatus == null)
            {
                return StatusCode(
                    500,
                    "Current booking status could not be found.");
            }


            // DIBA-specific state-transition rule:
            // Rejected, Cancelled and Completed bookings cannot be
            // cancelled again.
            if (currentStatus.StatusName.Equals(
                    "Rejected",
                    StringComparison.OrdinalIgnoreCase) ||

                currentStatus.StatusName.Equals(
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase) ||

                currentStatus.StatusName.Equals(
                    "Completed",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "This booking cannot be cancelled in its current status.");
            }


            // Change the booking status.
            booking.BookingStatusId =
                cancelledStatus.BookingStatusId;


            // DIBA audit logging:
            // cancellation records which authenticated user performed
            // the action and when it occurred.
            var auditLog = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = "Booking Cancelled",
                LogDescription =
                    $"Booking {booking.BookingId} was cancelled.",
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };

            _dbContext.AuditLogs.Add(auditLog);


            // DIBA notification workflow:
            // the organiser is informed when the booking is cancelled.
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                NotificationType = "Booking Cancelled",
                Message =
                    "Your venue booking has been cancelled.",
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                UserId = booking.UserId,
                BookingId = booking.BookingId
            };

            _dbContext.Notifications.Add(notification);

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Booking cancelled successfully.",
                bookingId =
                    booking.BookingId,
                status =
                    cancelledStatus.StatusName
            });
        }


        [HttpGet("venue/{venueId:guid}/availability")]
        public async Task<IActionResult> GetVenueAvailability(
    Guid venueId,
    DateTime date)
        {
            var venue = await dbContext.Venues
                .FirstOrDefaultAsync(v => v.VenueId == venueId);

            if (venue == null)
            {
                return NotFound("Venue not found.");
            }

            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var bookings = await dbContext.Bookings
                .Include(b => b.BookingStatus)
                .Where(b =>
                    b.VenueId == venueId &&
                    b.StartDateTime < dayEnd &&
                    b.EndDateTime > dayStart &&
                    b.BookingStatus != null &&
                    (
                        b.BookingStatus.StatusName == "Pending" ||
                        b.BookingStatus.StatusName == "Approved"
                    ))
                .Select(b => new VenueAvailabilityDto
                {
                    BookingId = b.BookingId,
                    StartDateTime = b.StartDateTime,
                    EndDateTime = b.EndDateTime,
                    StatusName = b.BookingStatus!.StatusName
                })
                .OrderBy(b => b.StartDateTime)
                .ToListAsync();

            return Ok(bookings);
        }
    }
}