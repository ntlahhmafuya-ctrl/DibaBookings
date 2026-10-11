using System.Net;
using System.Text;
using DIBA_Backend.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DIBA_Backend.Tests;

public class YocoPaymentServiceTests
{
    [Fact]
    public async Task CheckoutRequestIncludesCallerSuppliedIdempotencyKey()
    {
        var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Yoco:SecretKey"] = "test-secret",
                ["Yoco:SuccessUrl"] = "https://example.test/success",
                ["Yoco:CancelUrl"] = "https://example.test/cancel"
            })
            .Build();
        var service = new YocoPaymentService(httpClient, configuration);

        await service.CreateCheckoutAsync(125.50m, "DIBA-TEST-0001", "diba-checkout-test-0001");
        await service.CreateCheckoutAsync(125.50m, "DIBA-TEST-0001", "diba-checkout-test-0001");

        Assert.Equal(2, handler.IdempotencyKeys.Count);
        Assert.All(handler.IdempotencyKeys, key => Assert.Equal("diba-checkout-test-0001", key));
        Assert.Equal(2, handler.RequestBodies.Count);
        Assert.Contains("\"amount\":12550", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task CheckoutRequestRejectsMissingIdempotencyKey()
    {
        var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Yoco:SecretKey"] = "test-secret",
                ["Yoco:SuccessUrl"] = "https://example.test/success",
                ["Yoco:CancelUrl"] = "https://example.test/cancel"
            })
            .Build();
        var service = new YocoPaymentService(httpClient, configuration);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateCheckoutAsync(125.50m, "DIBA-TEST-0001", "  "));
        Assert.Empty(handler.IdempotencyKeys);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public List<string?> IdempotencyKeys { get; } = new();
        public List<string> RequestBodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            IdempotencyKeys.Add(
                request.Headers.TryGetValues("Idempotency-Key", out var values)
                    ? values.Single()
                    : null);
            RequestBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"id\":\"checkout_test_123\",\"redirectUrl\":\"https://checkout.yoco.com/test\",\"status\":\"created\"}",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
