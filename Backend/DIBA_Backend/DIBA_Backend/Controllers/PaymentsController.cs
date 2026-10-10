using DIBA_Backend.Data;
using DIBA_Backend.Dto.Payment;
using DIBA_Backend.Models.Entities;
using DIBA_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

        // Yoco calls this endpoint server-to-server. It must be configured
        // in the Yoco dashboard and reachable through public HTTPS.
        [AllowAnonymous]
        [HttpPost("yoco/webhook")]
        public async Task<IActionResult> YocoWebhook()
        {
            var webhookSecret = configuration["Yoco:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(webhookSecret) ||
                webhookSecret.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(500, "Yoco webhook secret is not configured.");
            }

            var webhookId = Request.Headers["webhook-id"].ToString();
            var webhookTimestamp = Request.Headers["webhook-timestamp"].ToString();
            var webhookSignature = Request.Headers["webhook-signature"].ToString();

            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            if (!IsValidYocoWebhookSignature(
                    webhookSecret,
                    webhookId,
                    webhookTimestamp,
                    webhookSignature,
                    rawBody))
            {
                return Unauthorized("Invalid webhook signature.");
            }

            try
            {
                using var document = JsonDocument.Parse(rawBody);
                var root = document.RootElement;

                if (!root.TryGetProperty("type", out var typeElement) ||
                    !root.TryGetProperty("payload", out var payload))
                {
                    return BadRequest("Webhook event is missing required fields.");
                }

                var eventType = typeElement.GetString();
                if (eventType is not ("payment.succeeded" or "payment.failed"))
                {
                    // Other event types are not handled by this endpoint.
                    return Ok();
                }

                if (!payload.TryGetProperty("amount", out var amountElement) ||
                    !amountElement.TryGetInt64(out var amountInCents) ||
                    !payload.TryGetProperty("currency", out var currencyElement) ||
                    !string.Equals(currencyElement.GetString(), "ZAR", StringComparison.OrdinalIgnoreCase) ||
                    !payload.TryGetProperty("metadata", out var metadata) ||
                    !metadata.TryGetProperty("reference", out var referenceElement))
                {
                    return BadRequest("Payment event is missing amount, currency, or reference metadata.");
                }

                var reference = referenceElement.GetString();
                if (string.IsNullOrWhiteSpace(reference))
                {
                    return BadRequest("Payment reference is missing.");
                }

                var payment = await dbContext.Payments
                    .FirstOrDefaultAsync(p => p.ReferenceNumber == reference);

                if (payment == null)
                {
                    // Return a failure so the provider can retry if the
                    // database record has not become visible yet.
                    return NotFound("Payment record was not found.");
                }

                var expectedAmountInCents = (long)Math.Round(
                    payment.Amount * 100,
                    MidpointRounding.AwayFromZero);

                if (amountInCents != expectedAmountInCents)
                {
                    return BadRequest("Webhook amount does not match the payment record.");
                }

                if (eventType == "payment.succeeded")
                {
                    // A success event must never be overwritten by a later
                    // failure event.
                    payment.PaymentStatus = "Succeeded";
                }
                else if (payment.PaymentStatus != "Succeeded" &&
                         payment.PaymentStatus == "Pending")
                {
                    payment.PaymentStatus = "Failed";
                }

                await dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (JsonException)
            {
                return BadRequest("Webhook body is not valid JSON.");
            }
            catch (InvalidOperationException)
            {
                return BadRequest("Webhook body contains invalid payment fields.");
            }
        }

        private static bool IsValidYocoWebhookSignature(
            string configuredSecret,
            string webhookId,
            string timestamp,
            string signatureHeader,
            string rawBody)
        {
            if (string.IsNullOrWhiteSpace(webhookId) ||
                string.IsNullOrWhiteSpace(timestamp) ||
                string.IsNullOrWhiteSpace(signatureHeader) ||
                !long.TryParse(timestamp, out var timestampSeconds))
            {
                return false;
            }

            // Reject old or future-dated webhook requests to reduce replay risk.
            var currentSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (timestampSeconds < currentSeconds - 300 ||
                timestampSeconds > currentSeconds + 300)
            {
                return false;
            }

            var secret = configuredSecret;
            if (secret.StartsWith("whsec_", StringComparison.Ordinal))
            {
                secret = secret["whsec_".Length..];
            }

            byte[] secretBytes;
            try
            {
                secretBytes = Convert.FromBase64String(secret);
            }
            catch (FormatException)
            {
                return false;
            }

            var signedContent = $"{webhookId}.{timestamp}.{rawBody}";
            using var hmac = new HMACSHA256(secretBytes);
            var expectedSignature = Convert.ToBase64String(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(signedContent)));

            var expectedBytes = Encoding.UTF8.GetBytes($"v1,{expectedSignature}");

            foreach (var candidate in signatureHeader.Split(
                         ' ',
                         StringSplitOptions.RemoveEmptyEntries))
            {
                if (candidate.StartsWith("v1,", StringComparison.Ordinal))
                {
                    var candidateBytes = Encoding.UTF8.GetBytes(candidate);
                    if (candidateBytes.Length == expectedBytes.Length &&
                        CryptographicOperations.FixedTimeEquals(
                            candidateBytes,
                            expectedBytes))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}