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
        var service = CreateService(httpClient);

        await service.CreateCheckoutAsync(125.50m, "DIBA-TEST-0001", "diba-checkout-test-0001");
        await service.CreateCheckoutAsync(125.50m, "DIBA-TEST-0001", "diba-checkout-test-0001");

        Assert.Equal(2, handler.IdempotencyKeys.Count);
        Assert.All(handler.IdempotencyKeys, key => Assert.Equal("diba-checkout-test-0001", key));
        Assert.Equal(2, handler.RequestBodies.Count);
        Assert.Contains("\"amount\":12550", handler.RequestBodies[0]);
        Assert.Contains("\"currency\":\"ZAR\"", handler.RequestBodies[0]);
        Assert.Contains("DIBA-TEST-0001", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task CheckoutRequestRejectsMissingIdempotencyKeyBeforeSendingRequest()
    {
        var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateCheckoutAsync(125.50m, "DIBA-TEST-0001", "  "));

        Assert.Empty(handler.IdempotencyKeys);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task CheckoutNonSuccessResponseThrowsWithoutPretendingCheckoutSucceeded(
        HttpStatusCode statusCode)
    {
        var handler = new RecordingHttpMessageHandler
        {
            Response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("provider error")
            }
        };
        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.CreateCheckoutAsync(125m, "DIBA-TEST-0002", "diba-checkout-test-0002"));

        Assert.Contains(((int)statusCode).ToString(), exception.Message);
        Assert.Single(handler.IdempotencyKeys);
    }

    [Fact]
    public async Task CheckoutNetworkTimeoutPropagatesForControllerToHandle()
    {
        var handler = new RecordingHttpMessageHandler
        {
            ExceptionToThrow = new TaskCanceledException("Simulated provider timeout.")
        };
        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient);

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            service.CreateCheckoutAsync(125m, "DIBA-TEST-0003", "diba-checkout-test-0003"));
    }

    [Fact]
    public async Task CheckoutMalformedProviderResponseDoesNotProduceValidCheckout()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json")
            }
        };
        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() =>
            service.CreateCheckoutAsync(125m, "DIBA-TEST-0004", "diba-checkout-test-0004"));
    }

    [Fact]
    public async Task RefundRequestUsesIdempotencyKeyAndAmountInCents()
    {
        var handler = new RecordingHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"id\":\"refund_test_123\",\"status\":\"succeeded\"}",
                    Encoding.UTF8,
                    "application/json")
            }
        };
        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient);

        var result = await service.RefundCheckoutAsync(
            "checkout/test-123", 42.75m, "DIBA-REFUND-0001", "diba-refund-test-0001");

        Assert.Equal("refund_test_123", result?.Id);
        Assert.Equal("diba-refund-test-0001", Assert.Single(handler.IdempotencyKeys));
        Assert.Contains("/api/checkouts/checkout%2Ftest-123/refund", handler.RequestUris.Single());
        Assert.Contains("\"amount\":4275", handler.RequestBodies.Single());
        Assert.Contains("DIBA-REFUND-0001", handler.RequestBodies.Single());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RefundRequestRejectsMissingCheckoutId(string checkoutId)
    {
        var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RefundCheckoutAsync(checkoutId, 20m, "DIBA-REFUND-0002", "diba-refund-test-0002"));

        Assert.Empty(handler.RequestUris);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RefundRequestRejectsZeroOrNegativeAmount(decimal amount)
    {
        var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.RefundCheckoutAsync("checkout_test_123", amount, "DIBA-REFUND-0003", "diba-refund-test-0003"));

        Assert.Empty(handler.RequestUris);
    }

    private static YocoPaymentService CreateService(HttpClient httpClient)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Yoco:SecretKey"] = "test-secret",
                ["Yoco:SuccessUrl"] = "https://example.test/success",
                ["Yoco:CancelUrl"] = "https://example.test/cancel"
            })
            .Build();

        return new YocoPaymentService(httpClient, configuration);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public List<string?> IdempotencyKeys { get; } = new();
        public List<string> RequestBodies { get; } = new();
        public List<string> RequestUris { get; } = new();
        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"id\":\"checkout_test_123\",\"redirectUrl\":\"https://checkout.yoco.com/test\",\"status\":\"created\"}",
                Encoding.UTF8,
                "application/json")
        };
        public Exception? ExceptionToThrow { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri?.AbsoluteUri ?? string.Empty);
            IdempotencyKeys.Add(
                request.Headers.TryGetValues("Idempotency-Key", out var values)
                    ? values.Single()
                    : null);
            RequestBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Response;
        }
    }
}
