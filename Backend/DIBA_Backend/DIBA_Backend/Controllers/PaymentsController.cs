using DIBA_Backend.Data;
using DIBA_Backend.Dto.Payment;
using DIBA_Backend.Models.Entities;
using DIBA_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
    // Similar logic: protecting controller actions so that only
    // authenticated users can access them.
    // DIBA adaptation: payment operations require authentication,
    // with additional role restrictions on individual actions.
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly DIBABookingsDbContext dbContext;
        private readonly YocoPaymentService yocoPaymentService;

        public PaymentsController(
            DIBABookingsDbContext dbContext,
            YocoPaymentService yocoPaymentService)
        {
            this.dbContext = dbContext;
            this.yocoPaymentService = yocoPaymentService;
        }

        // GET: api/Payments
        [HttpGet]

        // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
        // Similar logic: restricting an endpoint to specified roles.
        // DIBA adaptation: only Administrators and Staff can view
        // the complete list of payments.
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> GetPayments()
        {
            // Similar logic: projecting database records into a DTO
            // instead of directly exposing the database entity.
            // DIBA adaptation: PaymentResponseDto contains the payment
            // information required by the frontend.
            var payments = await dbContext.Payments
                .Select(p => new PaymentResponseDto
                {
                    PaymentId = p.PaymentId,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    ReferenceNumber = p.ReferenceNumber,
                    BookingId = p.BookingId
                })
                .ToListAsync();

            return Ok(payments);
        }

        // GET: api/Payments/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPayment(Guid id)
        {
            // Reference: Microsoft Learn, "Claims-based authorization
            // in ASP.NET Core".
            // Similar logic: obtaining the identity of the authenticated
            // user from a claim.
            // DIBA adaptation: the NameIdentifier claim contains the
            // UserId used to check ownership of the related booking.
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

            // Similar EF Core relationship-loading logic:
            // the related Booking is loaded because the payment access
            // decision depends on the booking owner.
            var payment = await dbContext.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(
                    p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound("Payment not found.");
            }

            // Reference: Microsoft Learn, "Role-based authorization
            // in ASP.NET Core".
            // Similar logic: checking whether the current user belongs
            // to a privileged role.
            // DIBA adaptation: Staff and Administrators can view payments
            // regardless of booking ownership.
            var isStaffOrAdmin =
                User.IsInRole("Staff") ||
                User.IsInRole("Administrator");

            // DIBA-specific ownership rule:
            // Event Organisers may only access a payment when the
            // related booking belongs to them.
            if (!isStaffOrAdmin &&
                (payment.Booking == null ||
                 payment.Booking.UserId != userId))
            {
                return Forbid();
            }

            var response = new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                ReferenceNumber = payment.ReferenceNumber,
                BookingId = payment.BookingId
            };

            return Ok(response);
        }

        // POST: api/Payments
        [HttpPost]

        // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
        // Similar logic: limiting an operation to a specific application role.
        // DIBA adaptation: only Event Organisers can create payment records
        // for their own approved bookings.
        [Authorize(Roles = "Event Organiser")]
        public async Task<IActionResult> CreatePayment(
    CreatePaymentDto createPaymentDto)
        {
            // Reference: Microsoft Learn, "Claims-based authorization
            // in ASP.NET Core".
            // Similar logic: identifying the authenticated user using
            // a claim.
            // DIBA adaptation: the UserId is used to verify ownership
            // of the booking being paid for.
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

<<<<<<< HEAD
            // DIBA-specific validation:
            // payment amounts must be positive before a payment record
            // can be created.
            if (createPaymentDto.Amount <= 0)
            {
                return BadRequest(
                    "Payment amount must be greater than zero.");
            }

            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar domain logic: retrieving a booking together with
            // its status so that the booking workflow can determine
            // what operations are currently allowed.
            // DIBA adaptation: the payment operation depends on the
            // booking status.
=======
>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc
            var booking = await dbContext.Bookings
                .Include(b => b.BookingStatus)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(
                    b => b.BookingId ==
                         createPaymentDto.BookingId);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            // DIBA-specific ownership rule:
            // the Event Organiser can only create a payment for their
            // own booking.
            if (booking.UserId != userId)
            {
                return Forbid();
            }

            if (booking.BookingStatus == null)
            {
                return StatusCode(
                    500,
                    "Booking status could not be determined.");
            }

<<<<<<< HEAD
            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: booking status controls what actions can
            // be performed on a booking.
            //
            // DIBA adaptation: a payment may only be recorded after
            // the booking has reached the Approved state.
=======
            // Only approved bookings can be paid
>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc
            if (!booking.BookingStatus.StatusName.Equals(
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Payment can only be made for an approved booking.");
            }

<<<<<<< HEAD
            // Similar duplicate-record prevention logic:
            // check whether a payment already exists before creating
            // another payment for the same booking.
            //
            // DIBA adaptation: each booking is restricted to one
            // payment record.
            var existingPayment =
                await dbContext.Payments
                    .AnyAsync(
                        p => p.BookingId ==
                             createPaymentDto.BookingId);
=======
            // Make sure the booking has a venue
            if (booking.Venue == null)
            {
                return BadRequest(
                    "The booking does not have a venue.");
            }
>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc

            // Get the payment amount from the venue
            var amount = booking.Venue.Price;

            if (amount <= 0)
            {
                return BadRequest(
                    "The venue does not have a valid price.");
            }

            // Check if payment already exists
            var existingPayment = await dbContext.Payments
                .FirstOrDefaultAsync(
                    p => p.BookingId == createPaymentDto.BookingId);

            if (existingPayment != null)
            {
                return Conflict(
                    "A payment already exists for this booking.");
            }

<<<<<<< HEAD
            // DIBA-specific payment creation.
            // A unique identifier and UTC timestamp are assigned when
            // the payment record is created.
=======
            // Generate DIBA payment reference
            var referenceNumber =
                $"DIBA-{DateTime.UtcNow:yyyyMMddHHmmss}";

            YocoCheckoutResponse? checkout;

            try
            {
                checkout = await yocoPaymentService.CreateCheckoutAsync(
                    amount,
                    referenceNumber);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    502,
                    new
                    {
                        message = "Unable to create Yoco checkout.",
                        error = ex.Message
                    });
            }

            if (checkout == null ||
                string.IsNullOrWhiteSpace(checkout.Id) ||
                string.IsNullOrWhiteSpace(checkout.RedirectUrl))
            {
                return StatusCode(
                    502,
                    "Yoco did not return a valid checkout.");
            }

>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc
            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
<<<<<<< HEAD
                ReferenceNumber =
                    createPaymentDto.ReferenceNumber,
                BookingId = createPaymentDto.BookingId
=======
                ReferenceNumber = referenceNumber,
                YocoCheckoutId = checkout.Id,
                PaymentStatus = "Pending",
                BookingId = booking.BookingId
>>>>>>> cb9fa948440e26bb53a2c1d4c8a47f2d8e685dbc
            };

            dbContext.Payments.Add(payment);

            await dbContext.SaveChangesAsync();

            return Ok(new
            {
                paymentId = payment.PaymentId,
                bookingId = payment.BookingId,
                venueName = booking.Venue.VenueName,
                amount = payment.Amount,
                referenceNumber = payment.ReferenceNumber,
                paymentStatus = payment.PaymentStatus,
                yocoCheckoutId = payment.YocoCheckoutId,
                checkoutUrl = checkout.RedirectUrl
            });
        }
    }
}