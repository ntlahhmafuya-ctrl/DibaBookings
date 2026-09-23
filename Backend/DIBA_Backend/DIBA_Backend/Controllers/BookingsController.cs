using DIBA_Backend.Data;
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
    public class BookingsController : ControllerBase
    {
        private readonly DIBABookingsDbContext dbContext;

        public BookingsController(DIBABookingsDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // GET: api/Bookings
        [HttpGet]
        public async Task<IActionResult> GetBookings()
        {
            var bookingsQuery = dbContext.Bookings.AsQueryable();
            if (!User.IsInRole("Staff") && !User.IsInRole("Administrator"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) return Unauthorized("User ID could not be determined.");
                bookingsQuery = bookingsQuery.Where(b => b.UserId == userId);
            }

            var bookings = await bookingsQuery
                .Include(b => b.User)
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .Include(b => b.BookingStatus)
                .Select(b => new BookingResponseDto
                {
                    BookingId = b.BookingId,
                    BookingDate = b.BookingDate,
                    StartDateTime = b.StartDateTime,
                    EndDateTime = b.EndDateTime,
                    SpecialRequirements = b.SpecialRequirements,
                    AdminNotes = b.AdminNotes,
                    UserId = b.UserId,
                    EventId = b.EventId,
                    VenueId = b.VenueId,
                    BookingStatusId = b.BookingStatusId,
                    StatusName = b.BookingStatus != null ? b.BookingStatus.StatusName : string.Empty,
                    OrganiserName = b.User != null ? b.User.FirstName + " " + b.User.LastName : string.Empty,
                    EventName = b.Event != null ? b.Event.EventName : string.Empty,
                    VenueName = b.Venue != null ? b.Venue.VenueName : string.Empty
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // GET: api/Bookings/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetBooking(Guid id)
        {
            var booking = await dbContext.Bookings
                .Include(b => b.User)
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .Include(b => b.BookingStatus)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            if (!User.IsInRole("Staff") && !User.IsInRole("Administrator"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId) || booking.UserId != userId) return Forbid();
            }

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
                StatusName = booking.BookingStatus?.StatusName ?? string.Empty,
                OrganiserName = booking.User == null ? string.Empty : booking.User.FirstName + " " + booking.User.LastName,
                EventName = booking.Event?.EventName ?? string.Empty,
                VenueName = booking.Venue?.VenueName ?? string.Empty
            };

            return Ok(response);
        }

        [HttpGet("availability")]
        public async Task<IActionResult> CheckAvailability(Guid venueId, DateTime startDateTime, DateTime endDateTime)
        {
            if (endDateTime <= startDateTime) return BadRequest("End date and time must be after the start date and time.");
            var venue = await dbContext.Venues.FirstOrDefaultAsync(v => v.VenueId == venueId);
            if (venue == null) return NotFound("Venue not found.");
            if (!venue.VenueStatus.Equals("Available", StringComparison.OrdinalIgnoreCase)) return Ok(new { available = false, reason = "The selected venue is not currently available." });

            var conflict = await dbContext.Bookings.AnyAsync(b =>
                b.VenueId == venueId &&
                b.StartDateTime < endDateTime &&
                b.EndDateTime > startDateTime &&
                b.BookingStatus != null &&
                (b.BookingStatus.StatusName == "Pending" || b.BookingStatus.StatusName == "Approved"));
            return Ok(new { available = !conflict, reason = conflict ? "The selected venue is already booked or awaiting approval for the requested time." : null });
        }

        // POST: api/Bookings
        [HttpPost]
        [Authorize(Roles = "Event Organiser")]
        public async Task<IActionResult> CreateBooking(
            CreateBookingDto createBookingDto)
        {
            // Get logged-in user's ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized("User ID could not be determined.");
            }

            if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            // Validate dates
            if (createBookingDto.EndDateTime <= createBookingDto.StartDateTime)
            {
                return BadRequest(
                    "End date and time must be after the start date and time.");
            }

            // Check that the event exists
            var eventEntity = await dbContext.Events
                .FirstOrDefaultAsync(e => e.EventId == createBookingDto.EventId);

            if (eventEntity == null)
            {
                return NotFound("Event not found.");
            }

            // Check that the venue exists
            var venue = await dbContext.Venues
                .FirstOrDefaultAsync(v => v.VenueId == createBookingDto.VenueId);

            if (venue == null)
            {
                return NotFound("Venue not found.");
            }

            // Check venue status
            if (!venue.VenueStatus.Equals(
                "Available",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("The selected venue is not currently available.");
            }

            // Check that the event belongs to the logged-in organiser
            if (eventEntity.UserId != userId)
            {
                return Forbid();
            }

            // Check for overlapping bookings
            var hasConflict = await dbContext.Bookings
                .AnyAsync(b =>
                    b.VenueId == createBookingDto.VenueId &&
                    b.StartDateTime < createBookingDto.EndDateTime &&
                    b.EndDateTime > createBookingDto.StartDateTime &&
                    b.BookingStatus != null &&
                    (
                        b.BookingStatus.StatusName == "Pending" ||
                        b.BookingStatus.StatusName == "Approved"
                    ));

            if (hasConflict)
            {
                return Conflict(
                    "The selected venue is already booked or awaiting approval for the requested time.");
            }

            // Find Pending status
            var pendingStatus = await dbContext.BookingStatuses
                .FirstOrDefaultAsync(bs => bs.StatusName == "Pending");

            if (pendingStatus == null)
            {
                return StatusCode(
                    500,
                    "Pending booking status could not be found.");
            }

            // Create booking
            var booking = new Booking
            {
                BookingId = Guid.NewGuid(),
                BookingDate = DateTime.UtcNow,
                StartDateTime = createBookingDto.StartDateTime,
                EndDateTime = createBookingDto.EndDateTime,
                SpecialRequirements = createBookingDto.SpecialRequirements,
                AdminNotes = null,
                UserId = userId,
                EventId = createBookingDto.EventId,
                VenueId = createBookingDto.VenueId,
                BookingStatusId = pendingStatus.BookingStatusId
            };

            dbContext.Bookings.Add(booking);

            await dbContext.SaveChangesAsync();

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
                BookingStatusId = booking.BookingStatusId
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
            // Get logged-in user's ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized("User ID could not be determined.");
            }

            if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            // Find booking
            var booking = await dbContext.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            // Make sure organiser owns the booking
            if (booking.UserId != userId)
            {
                return Forbid();
            }

            // Only pending bookings can be edited
            var currentStatus = await dbContext.BookingStatuses
                .FirstOrDefaultAsync(
                    bs => bs.BookingStatusId == booking.BookingStatusId);

            if (currentStatus == null)
            {
                return StatusCode(500, "Booking status could not be found.");
            }

            if (!currentStatus.StatusName.Equals(
                "Pending",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Only pending bookings can be updated.");
            }

            // Validate dates
            if (updateBookingDto.EndDateTime <= updateBookingDto.StartDateTime)
            {
                return BadRequest(
                    "End date and time must be after the start date and time.");
            }

            // Check event exists
            var eventEntity = await dbContext.Events
                .FirstOrDefaultAsync(
                    e => e.EventId == updateBookingDto.EventId);

            if (eventEntity == null)
            {
                return NotFound("Event not found.");
            }

            // Make sure the event belongs to the organiser
            if (eventEntity.UserId != userId)
            {
                return Forbid();
            }

            // Check venue exists
            var venue = await dbContext.Venues
                .FirstOrDefaultAsync(
                    v => v.VenueId == updateBookingDto.VenueId);

            if (venue == null)
            {
                return NotFound("Venue not found.");
            }

            // Check venue status
            if (!venue.VenueStatus.Equals(
                "Available",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "The selected venue is not currently available.");
            }

            // Check for booking conflicts
            // Exclude the current booking from the conflict check.
            var hasConflict = await dbContext.Bookings
                .AnyAsync(b =>
                    b.BookingId != id &&
                    b.VenueId == updateBookingDto.VenueId &&
                    b.StartDateTime < updateBookingDto.EndDateTime &&
                    b.EndDateTime > updateBookingDto.StartDateTime &&
                    b.BookingStatus != null &&
                    (
                        b.BookingStatus.StatusName == "Pending" ||
                        b.BookingStatus.StatusName == "Approved"
                    ));

            if (hasConflict)
            {
                return Conflict(
                    "The selected venue is already booked or awaiting approval for the requested time.");
            }

            // Update booking
            booking.EventId = updateBookingDto.EventId;
            booking.VenueId = updateBookingDto.VenueId;
            booking.StartDateTime = updateBookingDto.StartDateTime;
            booking.EndDateTime = updateBookingDto.EndDateTime;
            booking.SpecialRequirements =
                updateBookingDto.SpecialRequirements;

            await dbContext.SaveChangesAsync();

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
                BookingStatusId = booking.BookingStatusId
            };

            return Ok(response);
        }

        // PUT: api/Bookings/{id}/approve
        [HttpPut("{id:guid}/approve")]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> ApproveBooking(Guid id)
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

            var booking = await dbContext.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            var approvedStatus = await dbContext.BookingStatuses
                .FirstOrDefaultAsync(bs => bs.StatusName == "Approved");

            if (approvedStatus == null)
            {
                return StatusCode(500, "Approved booking status could not be found.");
            }

            var currentStatus = await dbContext.BookingStatuses
                .FirstOrDefaultAsync(bs => bs.BookingStatusId == booking.BookingStatusId);

            if (currentStatus == null)
            {
                return StatusCode(500, "Current booking status could not be found.");
            }

            if (!currentStatus.StatusName.Equals(
                "Pending",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only pending bookings can be approved.");
            }

            // Update booking status
            booking.BookingStatusId = approvedStatus.BookingStatusId;

            var auditLog = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = "Booking Approved",
                LogDescription = $"Booking {booking.BookingId} was approved.",
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };

            dbContext.AuditLogs.Add(auditLog);

            // Create notification for the Event Organiser
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                NotificationType = "Booking Approved",
                Message = "Your venue booking has been approved.",
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                UserId = booking.UserId,
                BookingId = booking.BookingId
            };

            dbContext.Notifications.Add(notification);

            await dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Booking approved successfully.",
                bookingId = booking.BookingId,
                status = approvedStatus.StatusName
            });
        }

        // PUT: api/Bookings/{id}/reject
        [HttpPut("{id:guid}/reject")]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> RejectBooking(
            Guid id,
            RejectBookingDto rejectBookingDto)
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

            var booking = await dbContext.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            var rejectedStatus = await dbContext.BookingStatuses
                .FirstOrDefaultAsync(bs => bs.StatusName == "Rejected");

            if (rejectedStatus == null)
            {
                return StatusCode(
                    500,
                    "Rejected booking status could not be found.");
            }

            var currentStatus = await dbContext.BookingStatuses
                .FirstOrDefaultAsync(
                    bs => bs.BookingStatusId == booking.BookingStatusId);

            if (currentStatus == null)
            {
                return StatusCode(
                    500,
                    "Current booking status could not be found.");
            }

            if (!currentStatus.StatusName.Equals(
                "Pending",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Only pending bookings can be rejected.");
            }

            // Validate rejection reason
            if (string.IsNullOrWhiteSpace(rejectBookingDto.Reason))
            {
                return BadRequest(
                    "A rejection reason is required.");
            }

            // Save the rejection reason
            booking.AdminNotes = rejectBookingDto.Reason;

            // Change booking status
            booking.BookingStatusId = rejectedStatus.BookingStatusId;

            var auditLog = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = "Booking Rejected",
                LogDescription = $"Booking {booking.BookingId} was rejected. Reason: {rejectBookingDto.Reason}",
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };

            dbContext.AuditLogs.Add(auditLog);

            // Create notification
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                NotificationType = "Booking Rejected",
                Message = $"Your venue booking has been rejected. Reason: {rejectBookingDto.Reason}",
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                UserId = booking.UserId,
                BookingId = booking.BookingId
            };

            dbContext.Notifications.Add(notification);

            await dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Booking rejected successfully.",
                bookingId = booking.BookingId,
                status = rejectedStatus.StatusName,
                reason = booking.AdminNotes
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

            var booking = await dbContext.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            var isStaffOrAdmin =
                User.IsInRole("Staff") ||
                User.IsInRole("Administrator");

            if (booking.UserId != userId && !isStaffOrAdmin)
            {
                return Forbid();
            }

            var cancelledStatus = await dbContext.BookingStatuses
                .FirstOrDefaultAsync(bs => bs.StatusName == "Cancelled");

            if (cancelledStatus == null)
            {
                return StatusCode(500, "Cancelled booking status could not be found.");
            }

            var currentStatus = await dbContext.BookingStatuses
                .FirstOrDefaultAsync(bs => bs.BookingStatusId == booking.BookingStatusId);

            if (currentStatus == null)
            {
                return StatusCode(500, "Current booking status could not be found.");
            }

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

            // Update booking status
            booking.BookingStatusId = cancelledStatus.BookingStatusId;

            var auditLog = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = "Booking Cancelled",
                LogDescription = $"Booking {booking.BookingId} was cancelled.",
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };

            dbContext.AuditLogs.Add(auditLog);

            // Create notification for the Event Organiser
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                NotificationType = "Booking Cancelled",
                Message = "Your venue booking has been cancelled.",
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                UserId = booking.UserId,
                BookingId = booking.BookingId
            };

            dbContext.Notifications.Add(notification);

            await dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Booking cancelled successfully.",
                bookingId = booking.BookingId,
                status = cancelledStatus.StatusName
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
