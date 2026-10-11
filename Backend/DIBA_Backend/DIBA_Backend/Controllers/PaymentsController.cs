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
        private readonly NotificationEmailService notificationEmailService;
        private readonly ILogger<PaymentsController> logger;

        public PaymentsController(
            DIBABookingsDbContext dbContext,
            YocoPaymentService yocoPaymentService,
            NotificationEmailService notificationEmailService,
            ILogger<PaymentsController> logger)
        {
            this.dbContext = dbContext;
            this.yocoPaymentService = yocoPaymentService;
            this.notificationEmailService = notificationEmailService;
            this.logger = logger;
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
                    PaymentStatus = p.PaymentStatus,
                    RefundAmount = p.RefundAmount,
                    RefundReason = p.RefundReason,
                    RefundStatus = p.RefundStatus,
                    RefundRequestedAtUtc = p.RefundRequestedAtUtc,
                    RefundProcessedAtUtc = p.RefundProcessedAtUtc,
                    YocoRefundId = p.YocoRefundId,
                    RefundFailureReason = p.RefundFailureReason
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
                PaymentStatus = payment.PaymentStatus,
                RefundAmount = payment.RefundAmount,
                RefundReason = payment.RefundReason,
                RefundStatus = payment.RefundStatus,
                RefundRequestedAtUtc = payment.RefundRequestedAtUtc,
                RefundProcessedAtUtc = payment.RefundProcessedAtUtc,
                YocoRefundId = payment.YocoRefundId,
                RefundFailureReason = payment.RefundFailureReason
            };

            return Ok(response);
        }

        // GET: api/Payments/booking/{bookingId}
        // Returns the most recent payment attempt for a booking the caller owns.
        [HttpGet("booking/{bookingId:guid}")]
        [Authorize(Roles = "Event Organiser")]
        public async Task<IActionResult> GetBookingPayment(Guid bookingId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("User ID could not be determined.");
            }

            var booking = await dbContext.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.BookingId == bookingId && item.UserId == userId);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            var payment = await dbContext.Payments
                .AsNoTracking()
                .Where(item => item.BookingId == bookingId)
                .OrderByDescending(item => item.PaymentDate)
                .Select(item => new PaymentResponseDto
                {
                    PaymentId = item.PaymentId,
                    Amount = item.Amount,
                    PaymentDate = item.PaymentDate,
                    ReferenceNumber = item.ReferenceNumber,
                    BookingId = item.BookingId,
                    PaymentStatus = item.PaymentStatus,
                    RefundAmount = item.RefundAmount,
                    RefundReason = item.RefundReason,
                    RefundStatus = item.RefundStatus,
                    RefundRequestedAtUtc = item.RefundRequestedAtUtc,
                    RefundProcessedAtUtc = item.RefundProcessedAtUtc,
                    YocoRefundId = item.YocoRefundId,
                    RefundFailureReason = item.RefundFailureReason
                })
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                return NotFound("No payment attempt was found for this booking.");
            }

            return Ok(payment);
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
            // Identify the logged-in organiser.
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized("User ID could not be determined or is invalid.");
            }

            // Load the booking and the information required for validation.
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
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(
                    "Payment can only be made for an approved booking.");
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

            // Check whether this booking already has an active payment.
            // A Pending attempt can be resumed using its original reference
            // and idempotency key. Other active statuses must not create a
            // second payment attempt.
            var payment = await dbContext.Payments
                .FirstOrDefaultAsync(p =>
                    p.BookingId == booking.BookingId &&
                    p.PaymentStatus != "Failed");

            string referenceNumber;
            string checkoutIdempotencyKey;

            if (payment != null)
            {
                if (!payment.PaymentStatus.Equals(
                        "Pending",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(
                        "A payment already exists for this booking.");
                }

                // Only references created using the current format can be
                // safely resumed automatically.
                var referencePrefix = $"DIBA-{booking.BookingId:N}-";

                if (string.IsNullOrWhiteSpace(payment.ReferenceNumber) ||
                    !payment.ReferenceNumber.StartsWith(
                        referencePrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(
                        "An existing payment attempt needs staff review. " +
                        "Please contact DIBA before trying again.");
                }

                var attemptSuffix = payment.ReferenceNumber[
                    referencePrefix.Length..];

                if (!int.TryParse(
                        attemptSuffix,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var attemptNumber) ||
                    attemptNumber < 1)
                {
                    return Conflict(
                        "The existing payment reference is invalid. " +
                        "Please contact DIBA staff.");
                }

                referenceNumber = payment.ReferenceNumber;

                checkoutIdempotencyKey =
                    $"diba-checkout-{booking.BookingId:N}-" +
                    attemptNumber.ToString(
                        "D4",
                        CultureInfo.InvariantCulture);

                // Keep the amount originally reserved for this attempt.
                // This also keeps a repeated Yoco request consistent.
                amount = payment.Amount;
            }
            else
            {
                // Determine the next attempt number.
                var failedAttemptCount = await dbContext.Payments
                    .AsNoTracking()
                    .CountAsync(p =>
                        p.BookingId == booking.BookingId &&
                        p.PaymentStatus == "Failed");

                var attemptNumber = failedAttemptCount + 1;

                var attemptSuffix = attemptNumber.ToString(
                    "D4",
                    CultureInfo.InvariantCulture);

                referenceNumber =
                    $"DIBA-{booking.BookingId:N}-{attemptSuffix}";

                checkoutIdempotencyKey =
                    $"diba-checkout-{booking.BookingId:N}-{attemptSuffix}";

                // IMPORTANT: reserve the payment slot in the database
                // BEFORE making an external request to Yoco.
                payment = new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    Amount = amount,
                    PaymentDate = DateTime.UtcNow,
                    ReferenceNumber = referenceNumber,
                    YocoCheckoutId = null,
                    PaymentStatus = "Pending",
                    BookingId = booking.BookingId
                };

                dbContext.Payments.Add(payment);

                try
                {
                    await dbContext.SaveChangesAsync();
                }
                catch (DbUpdateException exception)
                {
                    // The unique filtered index prevents two active
                    // payment records for the same booking.
                    var anotherActivePaymentExists =
                        await dbContext.Payments
                            .AsNoTracking()
                            .AnyAsync(p =>
                                p.BookingId == booking.BookingId &&
                                p.PaymentStatus != "Failed");

                    if (anotherActivePaymentExists)
                    {
                        return Conflict(
                            "A payment attempt already exists for this booking. " +
                            "Please refresh and check its status.");
                    }

                    logger.LogError(
                        exception,
                        "Could not reserve a payment for booking {BookingId}.",
                        booking.BookingId);

                    return StatusCode(
                        500,
                        "The payment reservation could not be saved. " +
                        "Please contact DIBA staff before retrying.");
                }
            }

            // Create or recover the Yoco checkout using the same idempotency
            // key for this payment attempt.
            YocoCheckoutResponse? checkout;

            try
            {
                checkout = await yocoPaymentService.CreateCheckoutAsync(
                    amount,
                    referenceNumber,
                    checkoutIdempotencyKey,
                    booking.BookingId);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Yoco checkout creation or recovery failed for booking {BookingId}, " +
                    "payment {PaymentId}.",
                    booking.BookingId,
                    payment.PaymentId);

                // Do NOT mark this payment Failed automatically.
                // Yoco may have created the checkout despite a timeout.
                // A retry can reuse the same reservation and idempotency key.
                return StatusCode(
                    502,
                    new
                    {
                        message =
                            "The checkout could not be confirmed. " +
                            "Your payment attempt has been retained. " +
                            "Please retry to recover the checkout or contact DIBA staff."
                    });
            }

            if (checkout == null ||
                string.IsNullOrWhiteSpace(checkout.Id) ||
                string.IsNullOrWhiteSpace(checkout.RedirectUrl))
            {
                // Retain the reservation because the outcome may be uncertain.
                return StatusCode(
                    502,
                    new
                    {
                        message =
                            "Yoco did not return a complete checkout response. " +
                            "The payment attempt has been retained for recovery."
                    });
            }

            // Never replace an already recorded checkout ID with a different one.
            if (!string.IsNullOrWhiteSpace(payment.YocoCheckoutId) &&
                !string.Equals(
                    payment.YocoCheckoutId,
                    checkout.Id,
                    StringComparison.Ordinal))
            {
                logger.LogError(
                    "Yoco returned a different checkout ID for payment {PaymentId}. " +
                    "Stored checkout: {StoredCheckoutId}; returned checkout: {ReturnedCheckoutId}.",
                    payment.PaymentId,
                    payment.YocoCheckoutId,
                    checkout.Id);

                return StatusCode(
                    502,
                    new
                    {
                        message =
                            "The checkout could not be safely matched to the existing " +
                            "payment attempt. Please contact DIBA staff."
                    });
            }

            // Save the checkout ID only after Yoco returns a valid response.
            if (string.IsNullOrWhiteSpace(payment.YocoCheckoutId))
            {
                payment.YocoCheckoutId = checkout.Id;

                try
                {
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Yoco created checkout {CheckoutId}, but DIBA could not " +
                        "save it for payment {PaymentId}.",
                        checkout.Id,
                        payment.PaymentId);

                    // Leave the reservation in place. Retrying with the same
                    // idempotency key can recover the original checkout.
                    return StatusCode(
                        500,
                        new
                        {
                            message =
                                "The checkout was created, but DIBA could not " +
                                "finish saving its details. Please retry to recover " +
                                "the checkout or contact DIBA staff."
                        });
                }
            }

            // Preserve the response fields expected by the existing frontend.
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


        // POST: api/Payments/{id}/refund/retry
        // Only staff/administrators may retry a refund that Yoco explicitly marked failed.
        // NeedsReview is deliberately excluded because the original request may have succeeded
        // even if DIBA did not receive its response.
        [HttpPost("{id:guid}/refund/retry")]
        [Authorize(Roles = "Staff,Administrator")]
        public async Task<IActionResult> RetryFailedRefund(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null ||
                !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("User ID could not be determined.");
            }

            var payment = await dbContext.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null)
            {
                return NotFound("Payment not found.");
            }

            if (!string.Equals(payment.PaymentStatus, "Succeeded", StringComparison.OrdinalIgnoreCase) ||
                payment.RefundAmount is null or <= 0 ||
                !string.Equals(payment.RefundStatus, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(
                    "Only a refund confirmed as failed by Yoco can be retried. Refunds marked NeedsReview must be reconciled with Yoco first.");
            }

            if (string.IsNullOrWhiteSpace(payment.YocoCheckoutId))
            {
                return BadRequest("The payment has no stored Yoco checkout ID.");
            }

            payment.RefundStatus = "Pending";
            payment.RefundRequestKey = $"diba-refund-{Guid.NewGuid():N}";
            payment.RefundRequestedAtUtc = DateTime.UtcNow;
            payment.RefundProcessedAtUtc = null;
            payment.RefundFailureReason = null;

            dbContext.AuditLogs.Add(new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = "Refund Retry Requested",
                LogDescription =
                    $"Staff/admin user {userId} retried refund for payment {payment.PaymentId}. " +
                    $"Amount: {payment.RefundAmount.Value:F2} ZAR.",
                Timestamp = DateTime.UtcNow,
                UserId = userId
            });

            await dbContext.SaveChangesAsync();

            try
            {
                var refund = await yocoPaymentService.RefundCheckoutAsync(
                    payment.YocoCheckoutId,
                    payment.RefundAmount.Value,
                    payment.ReferenceNumber ?? payment.PaymentId.ToString(),
                    payment.RefundRequestKey);

                payment.YocoRefundId = refund?.RefundId ?? refund?.Id ?? payment.YocoRefundId;

                if (string.Equals(refund?.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    payment.RefundStatus = "Succeeded";
                    payment.RefundProcessedAtUtc = DateTime.UtcNow;
                }
                else if (string.Equals(refund?.Status, "pending", StringComparison.OrdinalIgnoreCase))
                {
                    payment.RefundStatus = "Pending";
                }
                else
                {
                    payment.RefundStatus = "NeedsReview";
                    payment.RefundFailureReason =
                        "Yoco returned an unrecognised refund status. Reconcile with Yoco before retrying.";
                }
            }
            catch (Exception)
            {
                // A timeout does not prove the refund failed. Reconcile with Yoco before retrying again.
                payment.RefundStatus = "NeedsReview";
                payment.RefundFailureReason =
                    "The retry outcome could not be confirmed. Reconcile with Yoco before submitting another request.";
            }

            var notification = await dbContext.Notifications
                .Where(n =>
                    n.BookingId == payment.BookingId &&
                    n.NotificationType == "Booking Cancelled")
                .OrderByDescending(n => n.DateCreated)
                .FirstOrDefaultAsync();

            if (notification != null)
            {
                notification.Message = string.Equals(
                    payment.RefundStatus, "Succeeded", StringComparison.OrdinalIgnoreCase)
                    ? $"Your venue booking was cancelled. Yoco confirmed your refund of R{payment.RefundAmount.Value:F2}. Your bank may take additional time to show the funds."
                    : string.Equals(payment.RefundStatus, "Pending", StringComparison.OrdinalIgnoreCase)
                        ? $"Your venue booking was cancelled. Your refund of R{payment.RefundAmount.Value:F2} is being processed."
                        : $"Your venue booking was cancelled, but the refund of R{payment.RefundAmount.Value:F2} needs staff review.";
            }

            await dbContext.SaveChangesAsync();

            return Ok(new
            {
                paymentId = payment.PaymentId,
                refundAmount = payment.RefundAmount,
                refundStatus = payment.RefundStatus,
                yocoRefundId = payment.YocoRefundId,
                message = payment.RefundStatus == "Succeeded"
                    ? "Yoco confirmed the refund."
                    : payment.RefundStatus == "Pending"
                        ? "The refund is being processed."
                        : "The refund needs staff review before any further retry."
            });
        }

        // POST: api/Payments/yoco/webhook
        // Yoco calls this public endpoint after a payment status changes.
        // A webhook is trusted only after its signature and timestamp are verified.
        // POST: api/Payments/yoco/webhook
        // Yoco calls this endpoint after payment or refund status changes.
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

            using var reader = new StreamReader(
                Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();

            if (!IsValidYocoWebhookSignature(
                    webhookSecret,
                    webhookId,
                    webhookTimestamp,
                    webhookSignature,
                    rawBody))
            {
                return Unauthorized("Invalid Yoco webhook signature.");
            }

            // A verified delivery ID already in the database is a safe no-op.
            if (!string.IsNullOrWhiteSpace(webhookId) &&
                await dbContext.ProcessedYocoWebhooks
                    .AsNoTracking()
                    .AnyAsync(e => e.WebhookId == webhookId))
            {
                return Ok(new { received = true, duplicate = true });
            }

            try
            {
                using var document = JsonDocument.Parse(rawBody);
                var root = document.RootElement;
                var eventType = root.TryGetProperty("type", out var typeElement)
                    ? typeElement.GetString()
                    : null;

                if (eventType is not (
                    "payment.succeeded" or "payment.failed" or
                    "refund.succeeded" or "refund.failed"))
                {
                    return Ok(new { received = true, ignored = true });
                }

                // Yoco webhook payloads use "payload"; accept "data" as well so
                // a provider envelope variation does not leave a paid checkout Pending.
                var payload = root.TryGetProperty("payload", out var payloadElement)
                    ? payloadElement
                    : root.TryGetProperty("data", out var dataElement)
                        ? dataElement
                        : default;

                if (payload.ValueKind != JsonValueKind.Object ||
                    !payload.TryGetProperty("amount", out var amountElement) ||
                    !amountElement.TryGetInt64(out var amountInCents) ||
                    !payload.TryGetProperty("currency", out var currencyElement) ||
                    !string.Equals(currencyElement.GetString(), "ZAR", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Yoco webhook payload is missing required amount or currency.");
                }

                string? referenceNumber = null;
                if (payload.TryGetProperty("metadata", out var metadata) &&
                    metadata.ValueKind == JsonValueKind.Object &&
                    metadata.TryGetProperty("reference", out var referenceElement))
                {
                    referenceNumber = referenceElement.GetString();
                }

                string? checkoutId = null;
                if (payload.TryGetProperty("checkoutId", out var checkoutIdElement))
                {
                    checkoutId = checkoutIdElement.GetString();
                }
                if (string.IsNullOrWhiteSpace(checkoutId) &&
                    payload.TryGetProperty("metadata", out var checkoutMetadata) &&
                    checkoutMetadata.ValueKind == JsonValueKind.Object &&
                    checkoutMetadata.TryGetProperty("checkoutId", out var metadataCheckoutId))
                {
                    checkoutId = metadataCheckoutId.GetString();
                }

                var paymentQuery = dbContext.Payments.Include(p => p.Booking).AsQueryable();
                Payment? payment = null;
                if (!string.IsNullOrWhiteSpace(referenceNumber))
                {
                    payment = await paymentQuery.FirstOrDefaultAsync(
                        p => p.ReferenceNumber == referenceNumber);
                }

                if (payment == null && !string.IsNullOrWhiteSpace(checkoutId))
                {
                    payment = await paymentQuery.FirstOrDefaultAsync(
                        p => p.YocoCheckoutId == checkoutId);
                }

                if (payment == null)
                {
                    return StatusCode(500, "Payment record for this Yoco checkout/reference was not found.");
                }

                Notification? notificationToEmail = null;
                var previousPaymentStatus = payment.PaymentStatus;

                if (eventType is "payment.succeeded" or "payment.failed")
                {
                    var expectedPaymentCents = (long)Math.Round(
                        payment.Amount * 100,
                        MidpointRounding.AwayFromZero);

                    if (amountInCents != expectedPaymentCents)
                    {
                        return BadRequest("Yoco payment amount does not match the recorded booking amount.");
                    }

                    // A delayed failure event must never downgrade a successful payment.
                    if (eventType == "payment.succeeded")
                    {
                        payment.PaymentStatus = "Succeeded";
                    }
                    else if (string.Equals(payment.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                    {
                        payment.PaymentStatus = "Failed";
                    }

                    if (!string.Equals(previousPaymentStatus, payment.PaymentStatus, StringComparison.OrdinalIgnoreCase) &&
                        payment.Booking != null)
                    {
                        var message = string.Equals(payment.PaymentStatus, "Succeeded", StringComparison.OrdinalIgnoreCase)
                            ? "Yoco confirmed that your booking payment was successful."
                            : "Yoco reported that your booking payment failed. You may try again once the failed attempt is recorded.";

                        var alreadyNotified = await dbContext.Notifications.AnyAsync(n =>
                            n.BookingId == payment.BookingId &&
                            n.NotificationType == "Payment Status" &&
                            n.Message == message);

                        if (!alreadyNotified)
                        {
                            notificationToEmail = new Notification
                            {
                                NotificationId = Guid.NewGuid(),
                                NotificationType = "Payment Status",
                                Message = message,
                                DateCreated = DateTime.UtcNow,
                                IsRead = false,
                                UserId = payment.Booking.UserId,
                                BookingId = payment.BookingId
                            };
                            dbContext.Notifications.Add(notificationToEmail);
                        }
                    }
                }
                else
                {
                    var expectedRefundCents = (long)Math.Round(
                        (payment.RefundAmount ?? 0m) * 100,
                        MidpointRounding.AwayFromZero);

                    if (expectedRefundCents <= 0 ||
                        amountInCents != expectedRefundCents ||
                        string.IsNullOrWhiteSpace(payment.RefundStatus))
                    {
                        return BadRequest("Yoco refund amount does not match a recorded refund request.");
                    }

                    if (eventType == "refund.succeeded")
                    {
                        payment.RefundStatus = "Succeeded";
                        payment.RefundProcessedAtUtc = DateTime.UtcNow;

                        if (payload.TryGetProperty("id", out var refundIdElement))
                        {
                            payment.YocoRefundId = refundIdElement.GetString() ?? payment.YocoRefundId;
                        }

                        payment.RefundFailureReason = null;
                    }
                    else if (!string.Equals(
                        payment.RefundStatus, "Succeeded", StringComparison.OrdinalIgnoreCase))
                    {
                        payment.RefundStatus = "Failed";
                        payment.RefundProcessedAtUtc = DateTime.UtcNow;

                        if (payload.TryGetProperty("id", out var refundIdElement))
                        {
                            payment.YocoRefundId = refundIdElement.GetString() ?? payment.YocoRefundId;
                        }

                        payment.RefundFailureReason =
                            payload.TryGetProperty("failureReason", out var reasonElement)
                                ? reasonElement.GetString()
                                : "Yoco reported that the refund failed.";
                    }
                }

                if (eventType is "refund.succeeded" or "refund.failed")
                {
                    var cancellationNotification = await dbContext.Notifications
                        .Where(n =>
                            n.BookingId == payment.BookingId &&
                            n.NotificationType == "Booking Cancelled")
                        .OrderByDescending(n => n.DateCreated)
                        .FirstOrDefaultAsync();

                    if (cancellationNotification != null)
                    {
                        var previousMessage = cancellationNotification.Message;
                        if (string.Equals(
                            payment.RefundStatus, "Succeeded", StringComparison.OrdinalIgnoreCase))
                        {
                            cancellationNotification.Message =
                                $"Your venue booking was cancelled. Yoco confirmed your refund of R{payment.RefundAmount ?? 0m:F2}. Your bank may take additional time to show the funds.";
                        }
                        else if (string.Equals(
                            payment.RefundStatus, "Failed", StringComparison.OrdinalIgnoreCase))
                        {
                            cancellationNotification.Message =
                                $"Your venue booking was cancelled, but Yoco could not complete the refund of R{payment.RefundAmount ?? 0m:F2}. DIBA staff must review the refund.";
                        }

                        if (!string.Equals(previousMessage, cancellationNotification.Message, StringComparison.Ordinal))
                        {
                            notificationToEmail = cancellationNotification;
                        }
                    }
                }


                // The event ID is the idempotency key. The unique database
                // key prevents two concurrent deliveries from both applying.
                dbContext.ProcessedYocoWebhooks.Add(new ProcessedYocoWebhook
                {
                    WebhookId = webhookId,
                    EventType = eventType!,
                    ProcessedAtUtc = DateTime.UtcNow
                });

                await dbContext.SaveChangesAsync();

                if (notificationToEmail != null)
                {
                    await notificationEmailService.TrySendAsync(
                        notificationToEmail.UserId,
                        notificationToEmail.NotificationId,
                        notificationToEmail.NotificationType,
                        notificationToEmail.Message);
                }

                return Ok(new { received = true });
            }
            catch (JsonException)
            {
                return BadRequest("Yoco webhook body is not valid JSON.");
            }
            catch (DbUpdateException)
            {
                // A concurrent delivery may have inserted the same ID first.
                // Only treat it as a duplicate when that ID now exists.
                if (!string.IsNullOrWhiteSpace(webhookId) &&
                    await dbContext.ProcessedYocoWebhooks
                        .AsNoTracking()
                        .AnyAsync(e => e.WebhookId == webhookId))
                {
                    return Ok(new { received = true, duplicate = true });
                }

                return StatusCode(500, "Webhook could not be saved. Yoco may retry delivery.");
            }
        }

        private static bool IsValidYocoWebhookSignature(
            string secret,
            string webhookId,
            string timestamp,
            string signatureHeader,
            string rawBody)
        {
            if (string.IsNullOrWhiteSpace(webhookId) ||
                string.IsNullOrWhiteSpace(timestamp) ||
                string.IsNullOrWhiteSpace(signatureHeader) ||
                !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var timestampSeconds))
            {
                return false;
            }

            // Reject old or future-dated deliveries to reduce replay risk.
            DateTimeOffset timestampDate;
            try
            {
                timestampDate = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }

            if (Math.Abs((DateTimeOffset.UtcNow - timestampDate).TotalMinutes) > 5)
            {
                return false;
            }

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