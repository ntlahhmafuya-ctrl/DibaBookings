using DIBA_Backend.Data;
using DIBA_Backend.Dto.Booking;
using DIBA_Backend.Models.Entities;
using DIBA_Backend.Services;
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
        private readonly YocoPaymentService _yocoPaymentService;

        // DIBA policy: bookings must be made at least seven calendar days
        // before the event date. South Africa currently uses UTC+2 year-round.
        private static DateTime GetEarliestAllowedBookingDate()
        {
            return DateTime.UtcNow.AddHours(2).Date.AddDays(7);
        }

        private static bool IsWithinMinimumNoticePeriod(DateTime eventStart)
        {
            return eventStart.Date < GetEarliestAllowedBookingDate();
        }

        public BookingsController(
            DIBABookingsDbContext dbContext,
            YocoPaymentService yocoPaymentService)
        {
            _dbContext = dbContext;
            _yocoPaymentService = yocoPaymentService;
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


                    AcknowledgementAccepted =
        booking.AcknowledgementAccepted,

                    AcknowledgementAcceptedAt =
        booking.AcknowledgementAcceptedAt,

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

                AcknowledgementAccepted =
    booking.AcknowledgementAccepted,

                AcknowledgementAcceptedAt =
    booking.AcknowledgementAcceptedAt,

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

            if (IsWithinMinimumNoticePeriod(startDateTime))
            {
                return Ok(new
                {
                    available = false,
                    reason = "Bookings must be made at least 7 calendar days before the event date."
                });
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

            if (!createBookingDto.AcknowledgementAccepted)
            {
                return BadRequest(
                    "You must acknowledge the booking requirements before submitting the booking.");
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

            if (IsWithinMinimumNoticePeriod(createBookingDto.StartDateTime))
            {
                return BadRequest(
                    "Bookings must be made at least 7 calendar days before the event date. Please choose a later date.");
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
                    pendingStatus.BookingStatusId,
                        AcknowledgementAccepted = true,
                AcknowledgementAcceptedAt = DateTime.UtcNow
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
                    booking.BookingStatusId,

                AcknowledgementAccepted =
                    booking.AcknowledgementAccepted,

                AcknowledgementAcceptedAt =
                    booking.AcknowledgementAcceptedAt
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

            if (IsWithinMinimumNoticePeriod(updateBookingDto.StartDateTime))
            {
                return BadRequest(
                    "Bookings must be made at least 7 calendar days before the event date. Please choose a later date.");
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


            // Do not approve older pending bookings that violate the
            // minimum-notice policy.
            if (IsWithinMinimumNoticePeriod(booking.StartDateTime))
            {
                return BadRequest(
                    "This booking cannot be approved because bookings must be made at least 7 calendar days before the event date.");
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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("User ID could not be determined.");
            }

            if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            var booking = await _dbContext.Bookings
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            var isStaffOrAdministrator =
                User.IsInRole("Staff") || User.IsInRole("Administrator");

            if (booking.UserId != userId && !isStaffOrAdministrator)
            {
                return Forbid();
            }

            var currentStatus = await _dbContext.BookingStatuses
                .FirstOrDefaultAsync(s => s.BookingStatusId == booking.BookingStatusId);

            if (currentStatus == null)
            {
                return StatusCode(500, "Current booking status could not be determined.");
            }

            if (currentStatus.StatusName.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ||
                currentStatus.StatusName.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                currentStatus.StatusName.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("This booking cannot be cancelled in its current status.");
            }

            var payment = booking.Payments
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefault();

            // Do not cancel while Yoco is still deciding whether the payment succeeded.
            // Otherwise a late success could charge a cancelled booking without a refund.
            if (payment != null &&
                string.Equals(payment.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(
                    "The payment is still processing. Please wait for Yoco to confirm whether it succeeded before cancelling.");
            }

            var cancelledStatus = await _dbContext.BookingStatuses
                .FirstOrDefaultAsync(s => s.StatusName == "Cancelled");

            if (cancelledStatus == null)
            {
                return StatusCode(500, "Cancelled booking status could not be found.");
            }

            booking.BookingStatusId = cancelledStatus.BookingStatusId;

            var refundStatus = "NotRequired";
            decimal refundAmount = 0m;
            var refundReason = "No successful payment was found for this booking.";

            if (payment != null &&
                string.Equals(payment.PaymentStatus, "Succeeded", StringComparison.OrdinalIgnoreCase))
            {
                var nowSouthAfrica = DateTime.UtcNow.AddHours(2);
                var hoursUntilEvent = (booking.StartDateTime - nowSouthAfrica).TotalHours;

                // DIBA-initiated cancellations receive a full refund.
                // Organiser cancellations follow the proposed notice policy:
                // 168+ hours = 100%; 72-167.99 hours = 50%; under 72 hours = 0%.
                if (isStaffOrAdministrator)
                {
                    refundAmount = payment.Amount;
                    refundReason = "DIBA-initiated cancellation: full refund.";
                }
                else if (hoursUntilEvent >= 168)
                {
                    refundAmount = payment.Amount;
                    refundReason = "Organiser cancellation at least 7 days before the event: full refund.";
                }
                else if (hoursUntilEvent >= 72)
                {
                    refundAmount = Math.Round(
                        payment.Amount * 0.50m,
                        2,
                        MidpointRounding.AwayFromZero);
                    refundReason = "Organiser cancellation at least 72 hours but less than 7 days before the event: 50% refund.";
                }
                else
                {
                    refundAmount = 0m;
                    refundReason = "Organiser cancellation less than 72 hours before the event: no refund under the proposed policy.";
                }

                payment.RefundAmount = refundAmount;
                payment.RefundReason = refundReason;
                payment.RefundRequestedAtUtc = refundAmount > 0 ? DateTime.UtcNow : null;
                payment.RefundProcessedAtUtc = null;
                payment.RefundFailureReason = null;

                if (refundAmount <= 0)
                {
                    payment.RefundStatus = "NotEligible";
                    payment.RefundRequestKey = null;
                    refundStatus = payment.RefundStatus;
                }
                else
                {
                    payment.RefundStatus = "Pending";
                    payment.RefundRequestKey = $"diba-refund-{Guid.NewGuid():N}";
                    refundStatus = payment.RefundStatus;
                }
            }

            var auditLog = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = "Booking Cancelled",
                LogDescription =
                    $"Booking {booking.BookingId} was cancelled. " +
                    $"Refund status: {refundStatus}. " +
                    $"Refund amount: {refundAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} ZAR. " +
                    $"Reason: {refundReason}",
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };
            _dbContext.AuditLogs.Add(auditLog);

            var notificationMessage = refundAmount > 0
                ? $"Your venue booking has been cancelled. A refund of R{refundAmount:F2} has been requested. Refund status: {refundStatus}. Your bank may take additional time to show the funds after Yoco confirms the refund."
                : payment != null &&
                  string.Equals(payment.PaymentStatus, "Succeeded", StringComparison.OrdinalIgnoreCase)
                    ? $"Your venue booking has been cancelled. No refund is eligible under the current cancellation policy. Reason: {refundReason}"
                    : "Your venue booking has been cancelled. No refund is due because no successful payment was recorded.";

            _dbContext.Notifications.Add(new Notification
            {
                NotificationId = Guid.NewGuid(),
                NotificationType = "Booking Cancelled",
                Message = notificationMessage,
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                UserId = booking.UserId,
                BookingId = booking.BookingId
            });

            // Persist cancellation and refund intent before contacting Yoco.
            // This makes duplicate cancellation requests unable to start a second refund.
            await _dbContext.SaveChangesAsync();

            if (payment != null &&
                refundAmount > 0 &&
                string.Equals(payment.RefundStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(payment.YocoCheckoutId))
                {
                    payment.RefundStatus = "NeedsReview";
                    payment.RefundFailureReason =
                        "The successful payment has no stored Yoco checkout ID.";
                }
                else
                {
                    try
                    {
                        var yocoRefund = await _yocoPaymentService.RefundCheckoutAsync(
                            payment.YocoCheckoutId,
                            refundAmount,
                            payment.ReferenceNumber ?? payment.PaymentId.ToString(),
                            payment.RefundRequestKey!);

                        payment.YocoRefundId = yocoRefund?.RefundId;

                        if (string.Equals(yocoRefund?.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                        {
                            payment.RefundStatus = "Succeeded";
                            payment.RefundProcessedAtUtc = DateTime.UtcNow;
                            payment.RefundFailureReason = null;
                        }
                        else if (string.Equals(yocoRefund?.Status, "pending", StringComparison.OrdinalIgnoreCase))
                        {
                            payment.RefundStatus = "Pending";
                        }
                        else
                        {
                            payment.RefundStatus = "NeedsReview";
                            payment.RefundFailureReason =
                                "Yoco returned an unrecognised refund status. Check the Yoco dashboard before retrying.";
                        }
                    }
                    catch (Exception ex)
                    {
                        // The request may have reached Yoco even if the response was lost.
                        // Do not automatically retry; reconcile with Yoco first to avoid a duplicate refund.
                        payment.RefundStatus = "NeedsReview";
                        payment.RefundFailureReason =
                            $"Yoco refund outcome could not be confirmed. Reconcile with Yoco before retrying. Details: {ex.Message}";
                    }
                }

                await _dbContext.SaveChangesAsync();
                refundStatus = payment.RefundStatus ?? "NeedsReview";
            }

            return Ok(new
            {
                message = "Booking cancelled successfully.",
                bookingId = booking.BookingId,
                status = cancelledStatus.StatusName,
                refundAmount,
                refundStatus,
                refundReason
            });
        }


        [HttpGet("venue/{venueId:guid}/availability")]
        public async Task<IActionResult> GetVenueAvailability(
    Guid venueId,
    DateTime date)
        {
            var venue = await _dbContext.Venues
                .FirstOrDefaultAsync(v => v.VenueId == venueId);

            if (venue == null)
            {
                return NotFound("Venue not found.");
            }

            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var bookings = await _dbContext.Bookings
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