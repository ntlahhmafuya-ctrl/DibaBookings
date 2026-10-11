using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DIBA_Backend.Services;
using DIBA_Backend.Data;
using DIBA_Backend.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DIBA_Backend.Tests;

public class AccountSecurityTests
{
    private const string TestEmail = "security-test@diba.example";
    private const string OriginalPassword = "CurrentPassword123!";
    private const string NewPassword = "ACompletelyNewPassword456!";

    [Fact]
    public async Task PasswordChangeInvalidatesOldTokenAndNewLoginWorks()
    {
        using var factory = new DibaApiFactory();
        using var client = factory.CreateClient();
        var userId = await SeedUserAsync(factory);

        var token = await LoginAsync(client, OriginalPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var changeResponse = await client.PostAsJsonAsync("/api/Account/change-password", new
        {
            CurrentPassword = OriginalPassword,
            NewPassword
        });
        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);

        var oldTokenResponse = await client.GetAsync("/api/Users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, oldTokenResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var newToken = await LoginAsync(client, NewPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

        var newTokenResponse = await client.GetAsync("/api/Users/me");
        Assert.Equal(HttpStatusCode.OK, newTokenResponse.StatusCode);
    }

    [Fact]
    public async Task DeactivatingAccountInvalidatesPreviouslyIssuedToken()
    {
        using var factory = new DibaApiFactory();
        using var client = factory.CreateClient();
        var userId = await SeedUserAsync(factory);

        var token = await LoginAsync(client, OriginalPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DIBABookingsDbContext>();
            var user = await db.Users.SingleAsync(item => item.UserId == userId);
            user.IsActive = false;
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/Users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExistingActivePaymentBlocksAnotherCheckoutRequest()
    {
        using var factory = new DibaApiFactory();
        using var client = factory.CreateClient();
        var userId = await SeedUserAsync(factory);

        var venueId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var approvedStatusId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DIBABookingsDbContext>();
            db.Venues.Add(new Venue
            {
                VenueId = venueId,
                VenueName = "Payment Test Venue",
                VenueDescription = "Test-only venue",
                Capacity = 20,
                Price = 750m,
                Location = "Test",
                Latitude = 0,
                Longitude = 0,
                VenueStatus = "Available"
            });
            db.Events.Add(new Event
            {
                EventId = eventId,
                EventName = "Payment Test Event",
                EventDescription = "Test-only event",
                StartDateTime = DateTime.UtcNow.Date.AddDays(10).AddHours(9),
                EndDateTime = DateTime.UtcNow.Date.AddDays(10).AddHours(11),
                VenueId = venueId,
                UserId = userId
            });
            db.BookingStatuses.Add(new BookingStatus
            {
                BookingStatusId = approvedStatusId,
                StatusName = "Approved"
            });
            db.Bookings.Add(new Booking
            {
                BookingId = bookingId,
                BookingDate = DateTime.UtcNow,
                StartDateTime = DateTime.UtcNow.Date.AddDays(10).AddHours(9),
                EndDateTime = DateTime.UtcNow.Date.AddDays(10).AddHours(11),
                UserId = userId,
                EventId = eventId,
                VenueId = venueId,
                BookingStatusId = approvedStatusId,
                AcknowledgementAccepted = true,
                AcknowledgementAcceptedAt = DateTime.UtcNow
            });
            db.Payments.Add(new Payment
            {
                PaymentId = Guid.NewGuid(),
                Amount = 750m,
                PaymentDate = DateTime.UtcNow,
                ReferenceNumber = "DIBA-TEST-EXISTING",
                YocoCheckoutId = "checkout_existing_test",
                PaymentStatus = "Pending",
                BookingId = bookingId
            });
            await db.SaveChangesAsync();
        }

        var token = await LoginAsync(client, OriginalPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/Payments", new { BookingId = bookingId });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }


    [Fact]
    public async Task YocoTimeoutRetainsReservationAndRetryReusesIdempotencyKey()
    {
        using var factory = new DibaApiFactory();
        using var client = factory.CreateClient();
        var userId = await SeedUserAsync(factory);
        var bookingId = await SeedApprovedBookingAsync(factory, userId);

        // Simulate a timeout: Yoco may have created the checkout even though
        // DIBA did not receive a response. The payment must remain recoverable.
        factory.YocoHandler.ResponseFactory = _ =>
            throw new TaskCanceledException("Simulated Yoco checkout timeout.");

        var token = await LoginAsync(client, OriginalPassword);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var firstResponse = await client.PostAsJsonAsync(
            "/api/Payments",
            new { BookingId = bookingId });

        Assert.Equal(HttpStatusCode.BadGateway, firstResponse.StatusCode);

        Guid originalPaymentId;
        string? originalReference;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DIBABookingsDbContext>();
            var payment = await db.Payments.SingleAsync(
                item => item.BookingId == bookingId);

            Assert.Equal("Pending", payment.PaymentStatus);
            Assert.Null(payment.YocoCheckoutId);

            originalPaymentId = payment.PaymentId;
            originalReference = payment.ReferenceNumber;
        }

        // On retry, simulate Yoco returning the checkout associated with the
        // original idempotency key.
        factory.YocoHandler.ResponseFactory = _ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "id": "checkout_recovered_test",
                      "redirectUrl": "https://checkout.example/test",
                      "status": "created"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };

        var retryResponse = await client.PostAsJsonAsync(
            "/api/Payments",
            new { BookingId = bookingId });

        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DIBABookingsDbContext>();
            var payments = await db.Payments
                .Where(item => item.BookingId == bookingId)
                .ToListAsync();

            var recoveredPayment = Assert.Single(payments);
            Assert.Equal(originalPaymentId, recoveredPayment.PaymentId);
            Assert.Equal(originalReference, recoveredPayment.ReferenceNumber);
            Assert.Equal("Pending", recoveredPayment.PaymentStatus);
            Assert.Equal("checkout_recovered_test", recoveredPayment.YocoCheckoutId);
        }

        Assert.Equal(2, factory.YocoHandler.IdempotencyKeys.Count);
        Assert.False(string.IsNullOrWhiteSpace(factory.YocoHandler.IdempotencyKeys[0]));
        Assert.Equal(
            factory.YocoHandler.IdempotencyKeys[0],
            factory.YocoHandler.IdempotencyKeys[1]);
    }


    [Fact]
    public async Task YocoHttpErrorDoesNotMarkPaymentFailedOrReportSuccess()
    {
        using var factory = new DibaApiFactory();
        using var client = factory.CreateClient();
        var userId = await SeedUserAsync(factory);
        var bookingId = await SeedApprovedBookingAsync(factory, userId);

        factory.YocoHandler.ResponseFactory = _ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Yoco temporarily unavailable.")
            };

        var token = await LoginAsync(client, OriginalPassword);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(
            "/api/Payments",
            new { BookingId = bookingId });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DIBABookingsDbContext>();
            var payment = await db.Payments.SingleAsync(
                item => item.BookingId == bookingId);

            Assert.Equal("Pending", payment.PaymentStatus);
            Assert.Null(payment.YocoCheckoutId);
        }

        Assert.Single(factory.YocoHandler.IdempotencyKeys);
    }

    [Fact]
    public async Task OverlappingBookingForSameVenueIsRejected()
    {
        using var factory = new DibaApiFactory();
        using var client = factory.CreateClient();
        var userId = await SeedUserAsync(factory);

        var venueId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var pendingStatusId = Guid.NewGuid();
        var start = DateTime.UtcNow.Date.AddDays(10).AddHours(9);
        var end = start.AddHours(2);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DIBABookingsDbContext>();
            db.Venues.Add(new Venue
            {
                VenueId = venueId,
                VenueName = "Integration Test Venue",
                VenueDescription = "Test-only venue",
                Capacity = 30,
                Price = 1000m,
                Location = "Test",
                Latitude = 0,
                Longitude = 0,
                VenueStatus = "Available"
            });
            db.Events.Add(new Event
            {
                EventId = eventId,
                EventName = "Integration Test Event",
                EventDescription = "Test-only event",
                StartDateTime = start,
                EndDateTime = end,
                VenueId = venueId,
                UserId = userId
            });
            db.BookingStatuses.Add(new BookingStatus
            {
                BookingStatusId = pendingStatusId,
                StatusName = "Pending"
            });
            await db.SaveChangesAsync();
        }

        var token = await LoginAsync(client, OriginalPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookingPayload = new
        {
            EventId = eventId,
            VenueId = venueId,
            StartDateTime = start,
            EndDateTime = end,
            SpecialRequirements = "Integration test",
            AcknowledgementAccepted = true
        };

        var firstResponse = await client.PostAsJsonAsync("/api/Bookings", bookingPayload);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var overlappingResponse = await client.PostAsJsonAsync("/api/Bookings", new
        {
            EventId = eventId,
            VenueId = venueId,
            StartDateTime = start.AddMinutes(30),
            EndDateTime = end.AddMinutes(30),
            SpecialRequirements = "Overlapping integration test",
            AcknowledgementAccepted = true
        });
        Assert.Equal(HttpStatusCode.Conflict, overlappingResponse.StatusCode);
    }

    [Fact]
    public async Task LoginRateLimitRejectsExcessiveAttempts()
    {
        using var factory = new DibaApiFactory();
        using var client = factory.CreateClient();

        HttpResponseMessage? lastResponse = null;
        for (var attempt = 1; attempt <= 11; attempt++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/Authentication/login", new
            {
                Email = $"unknown-{attempt}@diba.example",
                Password = "WrongPassword123!"
            });

            if (attempt < 11)
            {
                Assert.NotEqual(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
            }
        }

        Assert.NotNull(lastResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
    }

    private static async Task<string> LoginAsync(HttpClient client, string password)
    {
        var response = await client.PostAsJsonAsync("/api/Authentication/login", new
        {
            Email = TestEmail,
            Password = password
        });
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Login failed with {(int)response.StatusCode} {response.StatusCode}: {responseBody}");

        using var document = JsonDocument.Parse(responseBody);
        return document.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Login response did not include a token.");
    }

    private static async Task<Guid> SeedUserAsync(DibaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DIBABookingsDbContext>();
        await db.Database.EnsureCreatedAsync();

        var role = new Role
        {
            RoleId = Guid.NewGuid(),
            RoleName = "Event Organiser"
        };
        var user = new User
        {
            UserId = Guid.NewGuid(),
            FirstName = "Security",
            LastName = "Test",
            Email = TestEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(OriginalPassword),
            RoleId = role.RoleId,
            Role = role,
            IsActive = true
        };

        db.Roles.Add(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.UserId;
    }


    private static async Task<Guid> SeedApprovedBookingAsync(
        DibaApiFactory factory,
        Guid userId)
    {
        var venueId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var approvedStatusId = Guid.NewGuid();
        var start = DateTime.UtcNow.Date.AddDays(10).AddHours(9);
        var end = start.AddHours(2);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DIBABookingsDbContext>();

        db.Venues.Add(new Venue
        {
            VenueId = venueId,
            VenueName = "Payment Recovery Test Venue",
            VenueDescription = "Test-only venue",
            Capacity = 20,
            Price = 750m,
            Location = "Test",
            Latitude = 0,
            Longitude = 0,
            VenueStatus = "Available"
        });

        db.Events.Add(new Event
        {
            EventId = eventId,
            EventName = "Payment Recovery Test Event",
            EventDescription = "Test-only event",
            StartDateTime = start,
            EndDateTime = end,
            VenueId = venueId,
            UserId = userId
        });

        db.BookingStatuses.Add(new BookingStatus
        {
            BookingStatusId = approvedStatusId,
            StatusName = "Approved"
        });

        db.Bookings.Add(new Booking
        {
            BookingId = bookingId,
            BookingDate = DateTime.UtcNow,
            StartDateTime = start,
            EndDateTime = end,
            UserId = userId,
            EventId = eventId,
            VenueId = venueId,
            BookingStatusId = approvedStatusId,
            AcknowledgementAccepted = true,
            AcknowledgementAcceptedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        return bookingId;
    }

    private sealed class FakeYocoHandler : HttpMessageHandler
    {
        public List<string> IdempotencyKeys { get; } = new();

        public Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "id": "checkout_default_test",
                      "redirectUrl": "https://checkout.example/test",
                      "status": "created"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Headers.TryGetValues("Idempotency-Key", out var values))
            {
                IdempotencyKeys.Add(values.Single());
            }

            return Task.FromResult(ResponseFactory(request));
        }
    }

    private sealed class DibaApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"DibaSecurityTests-{Guid.NewGuid():N}";

        public FakeYocoHandler YocoHandler { get; } = new();

        static DibaApiFactory()
        {
            // Minimal-host startup validates configuration before the test
            // server's later ConfigureWebHost callbacks are applied. Set
            // isolated test-only environment values before Program starts.
            Environment.SetEnvironmentVariable("Jwt__Key", "DIBA-Test-Signing-Key-At-Least-32-Bytes!");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "DIBA.Security.Tests");
            Environment.SetEnvironmentVariable("Jwt__Audience", "DIBA.Security.Tests");
            Environment.SetEnvironmentVariable("ConnectionStrings__DbConn",
                "Server=localhost;Database=DibaTests;Trusted_Connection=True;TrustServerCertificate=True");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "DIBA-Test-Signing-Key-At-Least-32-Bytes!",
                    ["Jwt:Issuer"] = "DIBA.Security.Tests",
                    ["Jwt:Audience"] = "DIBA.Security.Tests",
                    ["ConnectionStrings:DbConn"] = "Server=localhost;Database=DibaTests;Trusted_Connection=True;TrustServerCertificate=True",
                    ["Yoco:SecretKey"] = "test_yoco_secret_key",
                    ["Yoco:SuccessUrl"] = "https://example.test/payment/success",
                    ["Yoco:CancelUrl"] = "https://example.test/payment/cancel"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<DIBABookingsDbContext>>();
                services.AddDbContext<DIBABookingsDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName)
                        .ConfigureWarnings(warnings =>
                            warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

                // Override the production typed client's transport so tests never
                // contact Yoco and can simulate uncertain checkout outcomes.
                services.AddHttpClient<YocoPaymentService>()
                    .ConfigurePrimaryHttpMessageHandler(() => YocoHandler);
            });
        }
    }
}
