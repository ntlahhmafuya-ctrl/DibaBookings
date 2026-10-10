using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DIBA_Backend.Services
{
    public class YocoPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public YocoPaymentService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<YocoCheckoutResponse?> CreateCheckoutAsync(
            decimal amount,
            string reference)
        {
            var secretKey = _configuration["Yoco:SecretKey"];
            var successUrl = _configuration["Yoco:SuccessUrl"];
            var cancelUrl = _configuration["Yoco:CancelUrl"];

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException(
                    "Yoco secret key is not configured.");
            }

            var amountInCents = (long)Math.Round(
                amount * 100,
                MidpointRounding.AwayFromZero);

            var requestBody = new
            {
                amount = amountInCents,
                currency = "ZAR",
                successUrl = successUrl,
                cancelUrl = cancelUrl,
                metadata = new
                {
                    reference = reference
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://payments.yoco.com/api/checkouts");

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    secretKey);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Yoco checkout creation failed. " +
                    $"Status: {(int)response.StatusCode}. " +
                    $"Response: {responseContent}");
            }

            var result =
                JsonSerializer.Deserialize<YocoCheckoutResponse>(
                    responseContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            return result;
        }

        // Requests a full or partial refund for a completed Yoco checkout.
        // The idempotency key helps Yoco avoid creating the same refund twice.
        public async Task<YocoRefundResponse?> RefundCheckoutAsync(
            string checkoutId,
            decimal amount,
            string reference,
            string idempotencyKey)
        {
            var secretKey = _configuration["Yoco:SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException(
                    "Yoco secret key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(checkoutId))
            {
                throw new ArgumentException(
                    "Yoco checkout ID is required.", nameof(checkoutId));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount), "Refund amount must be greater than zero.");
            }

            var amountInCents = (long)Math.Round(
                amount * 100,
                MidpointRounding.AwayFromZero);

            var requestBody = new
            {
                amount = amountInCents,
                metadata = new { reference }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://payments.yoco.com/api/checkouts/{Uri.EscapeDataString(checkoutId)}/refund");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", secretKey);
            request.Headers.TryAddWithoutValidation(
                "Idempotency-Key", idempotencyKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Yoco refund request failed. Status: {(int)response.StatusCode}. " +
                    $"Response: {responseContent}");
            }

            var result = JsonSerializer.Deserialize<YocoRefundResponse>(
                responseContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result;
        }
    }

    public class YocoRefundResponse
    {
        public string? Id { get; set; }

        public string? RefundId { get; set; }

        public string? Status { get; set; }

        public string? Message { get; set; }
    }

    public class YocoCheckoutResponse
    {
        public string? Id { get; set; }

        public string? RedirectUrl { get; set; }

        public string? Status { get; set; }
    }
}