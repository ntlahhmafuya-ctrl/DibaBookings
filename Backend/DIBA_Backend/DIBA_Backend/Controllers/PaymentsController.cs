using DIBA_Backend.Data;
using DIBA_Backend.Dto.Payment;
using DIBA_Backend.Models.Entities;
using DIBA_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
                    BookingId = p.BookingId,
                    PaymentStatus = p.PaymentStatus
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
                BookingId = payment.BookingId,
                PaymentStatus = payment.PaymentStatus
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


            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar domain logic: retrieving a booking together with
            // its status so that the booking workflow can determine
            // what operations are currently allowed.
            // DIBA adaptation: the payment operation depends on the
            // booking status.
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

            // Reference: JedAngelo, "ConferenceBookingApi".
            // Similar logic: booking status controls what actions can
            // be performed on a booking.
            //
            // DIBA adaptation: a payment may only be recorded after
            // the booking has reached the Approved state.
            if (!booking.BookingStatus.StatusName.Equals(
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Payment can only be made for an approved booking.");
            }

            // Make sure the booking has a venue.
            if (booking.Venue == null)
            {
                return BadRequest(
                    "The booking does not have a venue.");
            }

            // Get the payment amount from the venue.
            var amount = booking.Venue.Price;

            if (amount <= 0)
            {
                return BadRequest(
                    "The venue does not have a valid price.");
            }

            // Check if payment already exists.
            // DIBA adaptation: each booking is restricted to one
            // payment record.
            var existingPayment = await dbContext.Payments
                .FirstOrDefaultAsync(
                    p => p.BookingId == createPaymentDto.BookingId);

            if (existingPayment != null)
            {
                return Conflict(
                    "A payment already exists for this booking.");
            }

            // Generate DIBA payment reference.
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

            // DIBA-specific payment creation.
            // A unique identifier and UTC timestamp are assigned when
            // the payment record is created.
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

        // POST: api/Payments/yoco/webhook
        // Yoco calls this public endpoint after a payment status changes.
        // A webhook is trusted only after its signature and timestamp are verified.
        [HttpPost("yoco/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleYocoWebhook()
        {
            var webhookSecret = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["Yoco:WebhookSecret"];

            if (string.IsNullOrWhiteSpace(webhookSecret) ||
                webhookSecret.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(503, "Yoco webhook verification is not configured.");
            }

            var webhookId = Request.Headers["webhook-id"].ToString();
            var webhookTimestamp = Request.Headers["webhook-timestamp"].ToString();
            var webhookSignature = Request.Headers["webhook-signature"].ToString();

            if (!IsValidYocoWebhookSignature(
                    webhookSecret,
                    webhookId,
                    webhookTimestamp,
                    webhookSignature,
                    Request.Body,
                    out var rawBody))
            {
                return Unauthorized("Invalid Yoco webhook signature.");
            }

            try
            {
                using var document = JsonDocument.Parse(rawBody);
                var root = document.RootElement;

                var eventType = root.TryGetProperty("type", out var typeElement)
                    ? typeElement.GetString()
                    : null;

                if (eventType is not ("payment.succeeded" or "payment.failed"))
                {
                    // Acknowledge unrelated event types without changing payment data.
                    return Ok(new { received = true, ignored = true });
                }

                if (!root.TryGetProperty("payload", out var payload) ||
                    !payload.TryGetProperty("amount", out var amountElement) ||
                    !amountElement.TryGetInt64(out var amountInCents) ||
                    !payload.TryGetProperty("currency", out var currencyElement) ||
                    !string.Equals(currencyElement.GetString(), "ZAR", StringComparison.OrdinalIgnoreCase) ||
                    !payload.TryGetProperty("metadata", out var metadata) ||
                    !metadata.TryGetProperty("checkoutId", out var checkoutIdElement))
                {
                    return BadRequest("Yoco webhook payload is missing required payment details.");
                }

                var checkoutId = checkoutIdElement.GetString();
                if (string.IsNullOrWhiteSpace(checkoutId))
                {
                    return BadRequest("Yoco checkout ID is missing.");
                }

                var payment = await dbContext.Payments
                    .FirstOrDefaultAsync(p => p.YocoCheckoutId == checkoutId);

                if (payment == null)
                {
                    // Return a retryable failure so a webhook arriving before
                    // the local payment record can be delivered again.
                    return StatusCode(500, "Payment record for this Yoco checkout was not found.");
                }

                var expectedAmountInCents = (long)Math.Round(
                    payment.Amount * 100,
                    MidpointRounding.AwayFromZero);

                if (amountInCents != expectedAmountInCents)
                {
                    return BadRequest("Yoco payment amount does not match the recorded booking amount.");
                }

                // Do not let duplicate or delayed failure events downgrade a successful payment.
                if (eventType == "payment.succeeded")
                {
                    payment.PaymentStatus = "Succeeded";
                }
                else if (string.Equals(payment.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                {
                    payment.PaymentStatus = "Failed";
                }

                await dbContext.SaveChangesAsync();

                return Ok(new { received = true });
            }
            catch (JsonException)
            {
                return BadRequest("Yoco webhook body is not valid JSON.");
            }
        }

        private static bool IsValidYocoWebhookSignature(
            string secret,
            string webhookId,
            string timestamp,
            string signatureHeader,
            Stream requestBody,
            out string rawBody)
        {
            rawBody = string.Empty;

            if (string.IsNullOrWhiteSpace(webhookId) ||
                string.IsNullOrWhiteSpace(timestamp) ||
                string.IsNullOrWhiteSpace(signatureHeader) ||
                !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var timestampSeconds))
            {
                return false;
            }

            // Reject old or future-dated deliveries to reduce replay risk.
            var timestampDate = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
            if (Math.Abs((DateTimeOffset.UtcNow - timestampDate).TotalMinutes) > 5)
            {
                return false;
            }

            using var reader = new StreamReader(requestBody, Encoding.UTF8, leaveOpen: true);
            rawBody = reader.ReadToEndAsync().GetAwaiter().GetResult();

            var secretValue = secret.StartsWith("whsec_", StringComparison.Ordinal)
                ? secret.Substring("whsec_".Length)
                : secret;

            byte[] secretBytes;
            try
            {
                secretBytes = Convert.FromBase64String(secretValue);
            }
            catch (FormatException)
            {
                return false;
            }

            var signedContent = $"{webhookId}.{timestamp}.{rawBody}";
            var expectedSignature = Convert.ToBase64String(
                HMACSHA256.HashData(secretBytes, Encoding.UTF8.GetBytes(signedContent)));

            foreach (var candidate in signatureHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = candidate.Split(',', 2);
                if (parts.Length == 2 &&
                    parts[0] == "v1" &&
                    CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(parts[1]),
                        Encoding.UTF8.GetBytes(expectedSignature)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}