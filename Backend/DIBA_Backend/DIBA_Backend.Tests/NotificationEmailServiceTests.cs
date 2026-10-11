using System.Net;
using System.Net.Http.Headers;
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
    public async Task MicrosoftGraphProviderRequestsTokenSendsMailAndRecordsSuccess()
    {
        var userId = Guid.NewGuid();
        var role = new Role { RoleId = Guid.NewGuid(), RoleName = "Event Organiser" };
        var options = new DbContextOptionsBuilder<DIBABookingsDbContext>()
            .UseInMemoryDatabase($"email-success-{Guid.NewGuid():N}")
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

        var handler = new FakeMicrosoftGraphHandler();
        var httpClient = new HttpClient(handler);
        var httpClientFactory = new SingleHttpClientFactory(httpClient);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Enabled"] = "true",
                ["Email:Provider"] = "MicrosoftGraph",
                ["Email:FromAddress"] = "bookings@example.test",
                ["Email:MicrosoftGraph:TenantId"] = "test-tenant",
                ["Email:MicrosoftGraph:ClientId"] = "test-client",
                ["Email:MicrosoftGraph:ClientSecret"] = "test-secret"
            })
            .Build();

        var service = new NotificationEmailService(
            db,
            configuration,
            NullLogger<NotificationEmailService>.Instance,
            httpClientFactory);

        var notificationId = Guid.NewGuid();
        await service.TrySendAsync(
            userId,
            notificationId,
            "Booking Approved",
            "Your booking has been approved.");

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("oauth2/v2.0/token", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Equal("https://graph.microsoft.com/v1.0/users/bookings%40example.test/sendMail",
            handler.Requests[1].RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer", handler.Requests[1].AuthorizationScheme);
        Assert.Equal("test-token", handler.Requests[1].AuthorizationParameter);

        using var messageJson = JsonDocument.Parse(handler.Requests[1].Body!);
        Assert.Equal("Booking Approved",
            messageJson.RootElement.GetProperty("message").GetProperty("subject").GetString());
        Assert.Equal("recipient@example.test",
            messageJson.RootElement.GetProperty("message")
                .GetProperty("toRecipients")[0]
                .GetProperty("emailAddress")
                .GetProperty("address")
                .GetString());

        var log = await db.EmailDeliveryLogs.SingleAsync();
        Assert.Equal("Sent", log.Status);
        Assert.Equal("recipient@example.test", log.RecipientEmail);
        Assert.Equal(notificationId, log.NotificationId);
        Assert.NotNull(log.SentAtUtc);
    }

    [Fact]
    public async Task MicrosoftGraphProviderWithMissingCredentialsRecordsSkipped()
    {
        var userId = Guid.NewGuid();
        var role = new Role { RoleId = Guid.NewGuid(), RoleName = "Event Organiser" };
        var options = new DbContextOptionsBuilder<DIBABookingsDbContext>()
            .UseInMemoryDatabase($"email-skipped-{Guid.NewGuid():N}")
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

        var handler = new FakeMicrosoftGraphHandler();
        var service = new NotificationEmailService(
            db,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Enabled"] = "true",
                ["Email:Provider"] = "MicrosoftGraph",
                ["Email:FromAddress"] = "bookings@example.test"
            }).Build(),
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

    private sealed class FakeMicrosoftGraphHandler : HttpMessageHandler
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
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                body));

            if (request.RequestUri!.Host == "login.microsoftonline.com")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"access_token":"test-token","token_type":"Bearer","expires_in":3600}""",
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }
    }

    private sealed record RecordedRequest(
        Uri RequestUri,
        string? AuthorizationScheme,
        string? AuthorizationParameter,
        string? Body);
}
