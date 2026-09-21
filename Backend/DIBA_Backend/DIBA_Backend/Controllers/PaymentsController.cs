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
                    BookingId = p.BookingId
                })
                .ToListAsync();

            return Ok(payments);
        }

        // GET: api/Payments/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPayment(Guid id)
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
        [Authorize(Roles = "Event Organiser")]
        public async Task<IActionResult> CreatePayment(
    CreatePaymentDto createPaymentDto)
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
                .Include(b => b.BookingStatus)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(
                    b => b.BookingId == createPaymentDto.BookingId);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            // Make sure the user owns the booking
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

            // Only approved bookings can be paid
            if (!booking.BookingStatus.StatusName.Equals(
                "Approved",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Payment can only be made for an approved booking.");
            }

            // Make sure the booking has a venue
            if (booking.Venue == null)
            {
                return BadRequest(
                    "The booking does not have a venue.");
            }

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

            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                ReferenceNumber = referenceNumber,
                YocoCheckoutId = checkout.Id,
                PaymentStatus = "Pending",
                BookingId = booking.BookingId
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
