using DIBA_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DIBA_Backend.Controllers
{
    // Yoco sends payment notifications directly to this public endpoint.
    // The signature is verified before the request body is trusted.
    [ApiController]
    [Route("api/Payments/yoco/webhook")]
    [AllowAnonymous]
    public class YocoWebhookController : ControllerBase
    {
        private readonly DIBABookingsDbContext dbContext;
        private readonly IConfiguration configuration;
        private readonly ILogger<YocoWebhookController> logger;

        public YocoWebhookController(
            DIBABookingsDbContext dbContext,
            IConfiguration configuration,
            ILogger<YocoWebhookController> logger)
        {
            this.dbContext = dbContext;
            this.configuration = configuration;
            this.logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            var webhookSecret = configuration["Yoco:WebhookSecret"];

            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                logger.LogError("Yoco webhook secret is not configured.");
                return StatusCode(500, "Yoco webhook secret is not configured.");
            }

            // Signature verification must use the exact, unmodified body bytes.
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var rawBody = await reader.ReadToEndAsync();

            var webhookId = Request.Headers["webhook-id"].ToString();
            var webhookTimestamp = Request.Headers["webhook-timestamp"].ToString();
            var webhookSignature = Request.Headers["webhook-signature"].ToString();

            if (!IsValidSignature(
                    webhookSecret,
                    webhookId,
                    webhookTimestamp,
                    webhookSignature,
                    rawBody))
            {
                logger.LogWarning("Rejected a Yoco webhook with an invalid signature.");
                return Unauthorized("Invalid webhook signature.");
            }

            try
            {
                using var document = JsonDocument.Parse(rawBody);
                var root = document.RootElement;

                if (!root.TryGetProperty("type", out var typeElement) ||
                    typeElement.ValueKind != JsonValueKind.String ||
                    !root.TryGetProperty("payload", out var payload) ||
                    payload.ValueKind != JsonValueKind.Object)
                {
                    return BadRequest("Webhook event is missing required fields.");
                }

                var eventType = typeElement.GetString();

                // A successful payment is confirmed only by payment.succeeded.
                // payment.created and unrelated event types must never mark a
                // DIBA payment as paid.
                if (eventType is not ("payment.succeeded" or "payment.failed"))
                {
                    return Ok();
                }

                if (!payload.TryGetProperty("amount", out var amountElement) ||
                    !amountElement.TryGetInt64(out var amountInCents) ||
                    !payload.TryGetProperty("currency", out var currencyElement) ||
                    currencyElement.ValueKind != JsonValueKind.String ||
                    !string.Equals(
                        currencyElement.GetString(),
                        "ZAR",
                        StringComparison.OrdinalIgnoreCase) ||
                    !payload.TryGetProperty("metadata", out var metadata) ||
                    metadata.ValueKind != JsonValueKind.Object ||
                    !metadata.TryGetProperty("reference", out var referenceElement) ||
                    referenceElement.ValueKind != JsonValueKind.String)
                {
                    return BadRequest(
                        "Payment event is missing valid amount, currency, or reference metadata.");
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
                    // A non-2xx response asks Yoco to retry delivery.
                    logger.LogWarning(
                        "Yoco webhook {WebhookId} referenced an unknown DIBA payment.",
                        webhookId);
                    return NotFound("Payment record was not found.");
                }

                var expectedAmountInCents = (long)Math.Round(
                    payment.Amount * 100,
                    MidpointRounding.AwayFromZero);

                if (amountInCents != expectedAmountInCents)
                {
                    logger.LogWarning(
                        "Yoco webhook {WebhookId} amount did not match payment {PaymentId}.",
                        webhookId,
                        payment.PaymentId);
                    return BadRequest("Webhook amount does not match the payment record.");
                }

                // Duplicate webhook deliveries are safe: setting the same
                // status more than once has no additional effect. A late
                // failure notification must not overwrite a confirmed success.
                if (eventType == "payment.succeeded")
                {
                    payment.PaymentStatus = "Succeeded";
                }
                else if (string.Equals(
                             payment.PaymentStatus,
                             "Pending",
                             StringComparison.OrdinalIgnoreCase))
                {
                    payment.PaymentStatus = "Failed";
                }

                await dbContext.SaveChangesAsync();

                logger.LogInformation(
                    "Processed Yoco webhook {WebhookId} for DIBA payment {PaymentId} with event {EventType}.",
                    webhookId,
                    payment.PaymentId,
                    eventType);

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

        private static bool IsValidSignature(
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

            // Reject stale or future-dated requests to limit replay attacks.
            var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (timestampSeconds < nowSeconds - 300 ||
                timestampSeconds > nowSeconds + 300)
            {
                return false;
            }

            // Yoco supplies a Standard Webhooks secret in whsec_<base64> form.
            var encodedSecret = configuredSecret.StartsWith(
                "whsec_",
                StringComparison.Ordinal)
                ? configuredSecret["whsec_".Length..]
                : configuredSecret;

            byte[] secretBytes;
            try
            {
                secretBytes = Convert.FromBase64String(encodedSecret);
            }
            catch (FormatException)
            {
                return false;
            }

            var signedContent = $"{webhookId}.{timestamp}.{rawBody}";
            using var hmac = new HMACSHA256(secretBytes);
            var calculatedSignature = Convert.ToBase64String(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(signedContent)));
            var expectedSignature = Encoding.UTF8.GetBytes($"v1,{calculatedSignature}");

            // The header can contain more than one v1 signature during key rotation.
            foreach (var candidate in signatureHeader.Split(
                         ' ',
                         StringSplitOptions.RemoveEmptyEntries))
            {
                if (!candidate.StartsWith("v1,", StringComparison.Ordinal))
                {
                    continue;
                }

                var candidateBytes = Encoding.UTF8.GetBytes(candidate);
                if (candidateBytes.Length == expectedSignature.Length &&
                    CryptographicOperations.FixedTimeEquals(
                        candidateBytes,
                        expectedSignature))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
