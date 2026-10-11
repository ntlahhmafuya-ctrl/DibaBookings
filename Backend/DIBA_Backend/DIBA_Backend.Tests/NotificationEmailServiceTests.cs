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
    public async Task BrevoProviderSendsMessageAndRecordsSuccess()
    {
        var userId = Guid.NewGuid();
        var role = new Role { RoleId = Guid.NewGuid(), RoleName = "Event Organiser" };
        var options = new DbContextOptionsBuilder<DIBABookingsDbContext>()
            .UseInMemoryDatabase($"brevo-success-{Guid.NewGuid():N}")
            .Options;

        await using var db = new DIBABookingsDbContext(options);
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

        var handler = new FakeBrevoHandler(HttpStatusCode.Accepted);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Enabled"] = "true",
                ["Email:Provider"] = "Brevo",
                ["Email:FromName"] = "DIBA Bookings",
                ["Email:FromAddress"] = "verified-sender@example.test",
                ["Email:Brevo:ApiKey"] = "test-api-key"
            })
            .Build();

        var service = new NotificationEmailService(
            db,
            configuration,
            NullLogger<NotificationEmailService>.Instance,
            new SingleHttpClientFactory(new HttpClient(handler)));

        var notificationId = Guid.NewGuid();
        await service.TrySendAsync(userId, notificationId, "Booking Approved", "Your booking was approved.");

        Assert.Single(handler.Requests);
        var request = handler.Requests[0];
        Assert.Equal("https://api.brevo.com/v3/smtp/email", request.Uri.AbsoluteUri);
        Assert.Equal("test-api-key", request.ApiKey);
        using var payload = JsonDocument.Parse(request.Body!);
        Assert.Equal("DIBA Bookings", payload.RootElement.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("verified-sender@example.test", payload.RootElement.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("recipient@example.test", payload.RootElement.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Booking Approved", payload.RootElement.GetProperty("subject").GetString());
        Assert.Equal("Your booking was approved.", payload.RootElement.GetProperty("textContent").GetString());

        var log = await db.EmailDeliveryLogs.SingleAsync();
        Assert.Equal("Sent", log.Status);
        Assert.Equal("recipient@example.test", log.RecipientEmail);
        Assert.Equal(notificationId, log.NotificationId);
        Assert.NotNull(log.SentAtUtc);
    }

    [Fact]
    public async Task BrevoProviderWithoutApiKeyRecordsSkippedAndDoesNotCallApi()
    {
        var userId = Guid.NewGuid();
        var role = new Role { RoleId = Guid.NewGuid(), RoleName = "Event Organiser" };
        var options = new DbContextOptionsBuilder<DIBABookingsDbContext>()
            .UseInMemoryDatabase($"brevo-skipped-{Guid.NewGuid():N}")
            .Options;

        await using var db = new DIBABookingsDbContext(options);
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

        var handler = new FakeBrevoHandler(HttpStatusCode.Accepted);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Enabled"] = "true",
                ["Email:Provider"] = "Brevo",
                ["Email:FromAddress"] = "verified-sender@example.test"
            })
            .Build();

        var service = new NotificationEmailService(
            db,
            configuration,
            NullLogger<NotificationEmailService>.Instance,
            new SingleHttpClientFactory(new HttpClient(handler)));

        await service.TrySendAsync(userId, Guid.NewGuid(), "Booking Approved", "Approved.");

        var log = await db.EmailDeliveryLogs.SingleAsync();
        Assert.Equal("Skipped", log.Status);
        Assert.Contains("settings are incomplete", log.ErrorMessage);
        Assert.Empty(handler.Requests);
    }

    private sealed class SingleHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class FakeBrevoHandler(HttpStatusCode responseStatus) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new RecordedRequest(
                request.RequestUri!,
                request.Headers.TryGetValues("api-key", out var values) ? values.Single() : null,
                body));

            return new HttpResponseMessage(responseStatus)
            {
                Content = new StringContent(
                    responseStatus == HttpStatusCode.Accepted
                        ? """{"messageId":"test-message-id"}"""
                        : """{"message":"invalid sender"}""",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed record RecordedRequest(Uri Uri, string? ApiKey, string? Body);
}
