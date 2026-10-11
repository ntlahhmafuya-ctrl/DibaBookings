using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DIBA_Backend.Data;
using DIBA_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Services
{
    /// <summary>
    /// Sends transactional notification emails through Brevo and records Sent,
    /// Failed, or Skipped outcomes. Email failure never undoes a saved booking.
    /// </summary>
    public sealed class NotificationEmailService
    {
        private readonly DIBABookingsDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationEmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public NotificationEmailService(
            DIBABookingsDbContext dbContext,
            IConfiguration configuration,
            ILogger<NotificationEmailService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task TrySendAsync(
            Guid userId,
            Guid? notificationId,
            string subject,
            string body)
        {
            string? recipient;
            try
            {
                recipient = await _dbContext.Users
                    .AsNoTracking()
                    .Where(user => user.UserId == userId)
                    .Select(user => user.Email)
                    .FirstOrDefaultAsync();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not look up the email recipient for notification {NotificationId}.", notificationId);
                return;
            }

            if (string.IsNullOrWhiteSpace(recipient))
            {
                await RecordOutcomeAsync(userId, notificationId, string.Empty, subject,
                    "Failed", "The notification recipient has no email address.", null);
                _logger.LogWarning("Notification email skipped because user {UserId} has no email address.", userId);
                return;
            }

            if (!_configuration.GetValue<bool>("Email:Enabled"))
            {
                await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                    "Skipped", "Email delivery is disabled.", null);
                _logger.LogInformation("Email delivery is disabled; notification {NotificationId} remains in-app only.", notificationId);
                return;
            }

            var provider = _configuration["Email:Provider"] ?? "Brevo";
            if (!string.Equals(provider, "Brevo", StringComparison.OrdinalIgnoreCase))
            {
                await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                    "Skipped", $"Unsupported email provider '{provider}'. Configure Email:Provider as Brevo.", null);
                _logger.LogWarning("Unsupported email provider {Provider}; notification {NotificationId} was not sent.", provider, notificationId);
                return;
            }

            var apiKey = _configuration["Email:Brevo:ApiKey"];
            var fromAddress = _configuration["Email:FromAddress"];
            var fromName = _configuration["Email:FromName"] ?? "DIBA Bookings";

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromAddress))
            {
                await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                    "Skipped", "Brevo email settings are incomplete. Set Email:Brevo:ApiKey and Email:FromAddress.", null);
                _logger.LogWarning("Brevo email settings are incomplete; notification {NotificationId} was not sent.", notificationId);
                return;
            }

            try
            {
                var payload = new
                {
                    sender = new { name = fromName, email = fromAddress },
                    to = new[] { new { email = recipient } },
                    subject,
                    textContent = body
                };

                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.brevo.com/v3/smtp/email");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("api-key", apiKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var httpClient = _httpClientFactory.CreateClient("Brevo");
                using var response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (responseBody.Length > 500)
                    {
                        responseBody = responseBody[..500];
                    }

                    throw new HttpRequestException(
                        $"Brevo returned HTTP {(int)response.StatusCode} ({response.StatusCode}). Response: {responseBody}",
                        null,
                        response.StatusCode);
                }

                await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                    "Sent", null, DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Brevo email delivery failed for notification {NotificationId}.", notificationId);
                await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                    "Failed", $"{exception.GetType().Name}: {exception.Message}", null);
            }
        }

        private async Task RecordOutcomeAsync(
            Guid userId,
            Guid? notificationId,
            string recipient,
            string subject,
            string status,
            string? errorMessage,
            DateTime? sentAtUtc)
        {
            try
            {
                var deliveryLog = new EmailDeliveryLog
                {
                    UserId = userId,
                    NotificationId = notificationId,
                    RecipientEmail = recipient.Length > 320 ? recipient[..320] : recipient,
                    Subject = subject.Length > 200 ? subject[..200] : subject,
                    Status = status,
                    ErrorMessage = errorMessage is null
                        ? null
                        : errorMessage[..Math.Min(errorMessage.Length, 1000)],
                    AttemptedAtUtc = DateTime.UtcNow,
                    SentAtUtc = sentAtUtc
                };

                _dbContext.EmailDeliveryLogs.Add(deliveryLog);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                var pendingLogs = _dbContext.ChangeTracker
                    .Entries<EmailDeliveryLog>()
                    .Where(entry => entry.State == EntityState.Added)
                    .ToList();

                foreach (var pendingLog in pendingLogs)
                {
                    pendingLog.State = EntityState.Detached;
                }

                _logger.LogError(exception, "Could not persist notification email delivery log.");
            }
        }
    }
}