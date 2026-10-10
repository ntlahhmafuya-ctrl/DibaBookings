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
            string reference,
            Guid paymentId)
        {
            var secretKey = _configuration["Yoco:SecretKey"];
            var configuredSuccessUrl = _configuration["Yoco:SuccessUrl"];
            var configuredCancelUrl = _configuration["Yoco:CancelUrl"];

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException(
                    "Yoco secret key is not configured.");
            }

            if (string.IsNullOrWhiteSpace(configuredSuccessUrl) ||
                string.IsNullOrWhiteSpace(configuredCancelUrl))
            {
                throw new InvalidOperationException(
                    "Yoco success and cancel URLs must be configured.");
            }

            // The return page needs the payment ID to retrieve the
            // authoritative status from the DIBA API.
            var successUrl = AddPaymentId(configuredSuccessUrl, paymentId);
            var cancelUrl = AddPaymentId(configuredCancelUrl, paymentId);

            var amountInCents = (long)Math.Round(
                amount * 100,
                MidpointRounding.AwayFromZero);

            var requestBody = new
            {
                amount = amountInCents,
                currency = "ZAR",
                successUrl,
                cancelUrl,
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
                new AuthenticationHeaderValue("Bearer", secretKey);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Do not return the provider response to the browser; it may
                // contain implementation details useful only in server logs.
                throw new HttpRequestException(
                    $"Yoco checkout creation failed with HTTP status {(int)response.StatusCode}.");
            }

            return JsonSerializer.Deserialize<YocoCheckoutResponse>(
                responseContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        private static string AddPaymentId(string url, Guid paymentId)
        {
            var separator = url.Contains('?') ? "&" : "?";
            return $"{url}{separator}paymentId={Uri.EscapeDataString(paymentId.ToString())}";
        }
    }

    public class YocoCheckoutResponse
    {
        public string? Id { get; set; }

        public string? RedirectUrl { get; set; }

        public string? Status { get; set; }
    }
}