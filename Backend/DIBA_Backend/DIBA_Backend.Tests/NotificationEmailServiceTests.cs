using System.Net;
using System.Text;
using System.Text.Json;
using DIBA_Backend.Data;
using DIBA_Backend.Models.Entities;
using DIBA_Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DIBA_Backend.Tests;

public class NotificationEmailServiceTests
{
    [Fact]
    public async Task BrevoProviderSendsTransactionalEmailAndRecordsSuccess()
    {
        var (db, userId) = await CreateDatabaseAsync();
        await using (db)
        {
            var handler = new FakeBrevoHandler(HttpStatusCode.Created, """{"messageId":"test-message-id"}""");
            var configuration = BuildConfiguration(enabled: true, apiKey: "test-api-key", fromAddress: "sender@example.test");
            var service = new NotificationEmailService(
                db, configuration, NullLogger<NotificationEmailService>.Instance,
                new SingleHttpClientFactory(new HttpClient(handler)));

            var notificationId = Guid.NewGuid();
            await service.TrySendAsync(userId, notificationId, "Booking Approved", "Your booking has been approved.");

            Assert.Single(handler.Requests);
            var request = handler.Requests[0];
            Assert.Equal("https://api.brevo.com/v3/smtp/email", request.RequestUri.AbsoluteUri);
            Assert.Equal("test-api-key", request.ApiKey);
            Assert.Equal("application/json", request.ContentType);

            using var json = JsonDocument.Parse(request.Body!);
            var root = json.RootElement;
            Assert.Equal("DIBA Bookings", root.GetProperty("sender").GetProperty("name").GetString());
            Assert.Equal("sender@example.test", root.GetProperty("sender").GetProperty("email").GetString());
            Assert.Equal("recipient@example.test", root.GetProperty("to")[0].GetProperty("email").GetString());
            Assert.Equal("Booking Approved", root.GetProperty("subject").GetString());
            Assert.Equal("Your booking has been approved.", root.GetProperty("textContent").GetString());

            var log = await db.EmailDeliveryLogs.SingleAsync();
            Assert.Equal("Sent", log.Status);
            Assert.Equal("recipient@example.test", log.RecipientEmail);
            Assert.Equal(notificationId, log.NotificationId);
            Assert.NotNull(log.SentAtUtc);
        }
    }

    [Fact]
    public async Task BrevoProviderWithMissingSettingsRecordsSkippedWithoutHttpRequest()
    {
        var (db, userId) = await CreateDatabaseAsync();
        await using (db)
        {
            var handler = new FakeBrevoHandler(HttpStatusCode.Created, "{}");
            var service = new NotificationEmailService(
                db,
                BuildConfiguration(enabled: true, apiKey: "", fromAddress: ""),
                NullLogger<NotificationEmailService>.Instance,
                new SingleHttpClientFactory(new HttpClient(handler)));

            await service.TrySendAsync(userId, Guid.NewGuid(), "Booking Approved", "Approved.");

            var log = await db.EmailDeliveryLogs.SingleAsync();
            Assert.Equal("Skipped", log.Status);
            Assert.Contains("settings are incomplete", log.ErrorMessage);
            Assert.Empty(handler.Requests);
        }
    }

    [Fact]
    public async Task BrevoApiErrorRecordsFailedDelivery()
    {
        var (db, userId) = await CreateDatabaseAsync();
        await using (db)
        {
            var handler = new FakeBrevoHandler(HttpStatusCode.Unauthorized, """{"message":"Invalid API key"}""");
            var service = new NotificationEmailService(
                db,
                BuildConfiguration(enabled: true, apiKey: "bad-key", fromAddress: "sender@example.test"),
                NullLogger<NotificationEmailService>.Instance,
                new SingleHttpClientFactory(new HttpClient(handler)));

            await service.TrySendAsync(userId, Guid.NewGuid(), "Booking Approved", "Approved.");

            var log = await db.EmailDeliveryLogs.SingleAsync();
            Assert.Equal("Failed", log.Status);
            Assert.Contains("HTTP 401", log.ErrorMessage);
        }
    }

    private static IConfiguration BuildConfiguration(bool enabled, string apiKey, string fromAddress) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Email:Enabled"] = enabled.ToString(),
            ["Email:Provider"] = "Brevo",
            ["Email:FromAddress"] = fromAddress,
            ["Email:FromName"] = "DIBA Bookings",
            ["Email:Brevo:ApiKey"] = apiKey
        }).Build();

    private static async Task<(DIBABookingsDbContext Db, Guid UserId)> CreateDatabaseAsync()
    {
        var userId = Guid.NewGuid();
        var role = new Role { RoleId = Guid.NewGuid(), RoleName = "Event Organiser" };
        var options = new DbContextOptionsBuilder<DIBABookingsDbContext>()
            .UseInMemoryDatabase($"brevo-email-{Guid.NewGuid():N}")
            .Options;
        var db = new DIBABookingsDbContext(options);
        db.Roles.Add(role);
        db.Users.Add(new User
        {
            UserId = userId,
            FirstName = "Test",
            LastName = "Organiser",
            Email = "recipient@example.test",
            PasswordHash = "test-hash",
            RoleId = role.RoleId,
            Role = role
        });
        await db.SaveChangesAsync();
        return (db, userId);
    }

    private sealed class SingleHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class FakeBrevoHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.RequestUri!,
                request.Headers.TryGetValues("api-key", out var keys) ? keys.Single() : null,
                request.Content?.Headers.ContentType?.MediaType,
                body));
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed record RecordedRequest(Uri RequestUri, string? ApiKey, string? ContentType, string? Body);
}