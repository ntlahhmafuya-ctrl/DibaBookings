using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

    private sealed class DibaApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"DibaSecurityTests-{Guid.NewGuid():N}";

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
                    ["ConnectionStrings:DbConn"] = "Server=localhost;Database=DibaTests;Trusted_Connection=True;TrustServerCertificate=True"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<DIBABookingsDbContext>>();
                services.AddDbContext<DIBABookingsDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName)
                        .ConfigureWarnings(warnings =>
                            warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            });
        }
    }
}
