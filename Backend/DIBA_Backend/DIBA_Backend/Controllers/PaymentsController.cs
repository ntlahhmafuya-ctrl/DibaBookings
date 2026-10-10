using DIBA_Backend.Data;
using DIBA_Backend.Dto.Payment;
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
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly DIBABookingsDbContext dbContext;
        private readonly YocoPaymentService yocoPaymentService;
        private readonly IConfiguration configuration;

        public PaymentsController(
            DIBABookingsDbContext dbContext,
            YocoPaymentService yocoPaymentService,
            IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.yocoPaymentService = yocoPaymentService;
            this.configuration = configuration;
        }

        // Only Staff and Administrators can see the full payment list.
        [HttpGet]
        [Authorize(Roles = "Administrator,Staff")]
        public async Task<IActionResult> GetPayments()
        {
            var payments = await dbContext.Payments
                .Select(p => new PaymentResponseDto
                {
                    PaymentId = p.PaymentId,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    ReferenceNumber = p.ReferenceNumber,
                    BookingId = p.BookingId,
                    PaymentStatus = p.PaymentStatus
                })
                .ToListAsync();

            return Ok(payments);
        }

        // The organiser can only see payments belonging to their own bookings.
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPayment(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("User ID could not be determined.");
            }

            var payment = await dbContext.Payments
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound("Payment not found.");
            }

            var isStaffOrAdmin =
                User.IsInRole("Staff") ||
                User.IsInRole("Administrator");

            if (!isStaffOrAdmin &&
                (payment.Booking == null || payment.Booking.UserId != userId))
            {
                return Forbid();
            }

            return Ok(new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                ReferenceNumber = payment.ReferenceNumber,
                BookingId = payment.BookingId,
                PaymentStatus = payment.PaymentStatus
            });
        }

        [HttpPost]
        [Authorize(Roles = "Event Organiser")]
        public async Task<IActionResult> CreatePayment(
            CreatePaymentDto createPaymentDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("User ID could not be determined.");
            }

            var booking = await dbContext.Bookings
                .Include(b => b.BookingStatus)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(
                    b => b.BookingId == createPaymentDto.BookingId);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            if (booking.UserId != userId)
            {
                return Forbid();
            }

            if (booking.BookingStatus == null)
            {
                return StatusCode(500, "Booking status could not be determined.");
            }

            if (!booking.BookingStatus.StatusName.Equals(
                    "Approved", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Payment can only be made for an approved booking.");
            }

            if (booking.Venue == null)
            {
                return BadRequest("The booking does not have a venue.");
            }

            var amount = booking.Venue.Price;
            if (amount <= 0)
            {
                return BadRequest("The venue does not have a valid price.");
            }

            var existingPayment = await dbContext.Payments
                .FirstOrDefaultAsync(p => p.BookingId == createPaymentDto.BookingId);

            if (existingPayment != null)
            {
                return Conflict("A payment already exists for this booking.");
            }

            // Save the pending record before creating checkout so a webhook
            // cannot arrive before the payment reference exists in the database.
            var paymentId = Guid.NewGuid();
            var referenceNumber =
                $"DIBA-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30];

            var payment = new Payment
            {
                PaymentId = paymentId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                ReferenceNumber = referenceNumber,
                PaymentStatus = "Pending",
                BookingId = booking.BookingId
            };

            dbContext.Payments.Add(payment);
            await dbContext.SaveChangesAsync();

            YocoCheckoutResponse? checkout;
            try
            {
                checkout = await yocoPaymentService.CreateCheckoutAsync(
                    amount,
                    referenceNumber,
                    paymentId);
            }
            catch (Exception)
            {
                // No usable checkout was returned, so remove the temporary
                // record and allow the organiser to retry.
                dbContext.Payments.Remove(payment);
                await dbContext.SaveChangesAsync();

                return StatusCode(
                    502,
                    new { message = "Unable to create Yoco checkout. Please try again." });
            }

            if (checkout == null ||
                string.IsNullOrWhiteSpace(checkout.Id) ||
                string.IsNullOrWhiteSpace(checkout.RedirectUrl))
            {
                dbContext.Payments.Remove(payment);
                await dbContext.SaveChangesAsync();

                return StatusCode(502, "Yoco did not return a valid checkout.");
            }

            payment.YocoCheckoutId = checkout.Id;
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