using System.Net;
using System.Net.Mail;
using System.Net.Http.Json;
using DIBA_Backend.Data;
using DIBA_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Services
{
    /// <summary>
    /// Sends notification emails through Brevo transactional email or optional SMTP
    /// and records Sent, Failed, or Skipped outcomes. Email failure never undoes a saved booking.
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

        public async Task TrySendAsync(Guid userId, Guid? notificationId, string subject, string body)
        {
            string? recipient;
            try
            {
                recipient = await _dbContext.Users.AsNoTracking()
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

            var provider = _configuration["Email:Provider"] ?? "Smtp";
            var fromAddress = _configuration["Email:FromAddress"];

            try
            {
                if (string.Equals(provider, "Brevo", StringComparison.OrdinalIgnoreCase))
                {
                    var apiKey = _configuration["Email:Brevo:ApiKey"];
                    if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromAddress))
                    {
                        await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                            "Skipped", "Brevo email settings are incomplete. Configure the API key and a verified sender address.", null);
                        _logger.LogWarning("Brevo email settings are incomplete; notification {NotificationId} was not sent.", notificationId);
                        return;
                    }

                    var senderName = _configuration["Email:FromName"] ?? "DIBA Bookings";
                    await SendWithBrevoAsync(apiKey, fromAddress, senderName, recipient, subject, body);
                }
                else if (string.Equals(provider, "Smtp", StringComparison.OrdinalIgnoreCase))
                {
                    var host = _configuration["Email:SmtpHost"];
                    if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
                    {
                        await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                            "Skipped", "SMTP email settings are incomplete.", null);
                        _logger.LogWarning("SMTP email settings are incomplete; notification {NotificationId} was not sent.", notificationId);
                        return;
                    }

                    var port = _configuration.GetValue<int?>("Email:Port") ?? 587;
                    var enableSsl = _configuration.GetValue<bool?>("Email:EnableSsl") ?? true;
                    var username = _configuration["Email:Username"];
                    var password = _configuration["Email:Password"];

                    using var client = new SmtpClient(host, port)
                    {
                        EnableSsl = enableSsl,
                        UseDefaultCredentials = false,
                        Credentials = string.IsNullOrWhiteSpace(username)
                            ? null
                            : new NetworkCredential(username, password)
                    };
                    using var message = new MailMessage(fromAddress, recipient, subject, body);
                    await client.SendMailAsync(message);
                }
                else
                {
                    await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                        "Skipped", $"Unsupported email provider '{provider}'. Use Brevo or Smtp.", null);
                    return;
                }

                await RecordOutcomeAsync(userId, notificationId, recipient, subject, "Sent", null, DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Notification email delivery failed for notification {NotificationId}.", notificationId);
                await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                    "Failed", $"{exception.GetType().Name}: {exception.Message}", null);
            }
        }

        private async Task SendWithBrevoAsync(
            string apiKey, string fromAddress, string senderName,
            string recipient, string subject, string body)
        {
            var httpClient = _httpClientFactory.CreateClient("Brevo");
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Headers.Accept.ParseAdd("application/json");
            request.Content = JsonContent.Create(new
            {
                sender = new { name = senderName, email = fromAddress },
                to = new[] { new { email = recipient } },
                subject,
                textContent = body
            });

            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync();
                var details = string.IsNullOrWhiteSpace(responseText)
                    ? response.ReasonPhrase ?? "Brevo returned an error."
                    : responseText;
                throw new HttpRequestException($"Brevo API returned HTTP {(int)response.StatusCode}: {details}");
            }
        }

        private async Task RecordOutcomeAsync(
            Guid userId, Guid? notificationId, string recipient, string subject,
            string status, string? errorMessage, DateTime? sentAtUtc)
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
                    ErrorMessage = errorMessage is null ? null : errorMessage[..Math.Min(errorMessage.Length, 1000)],
                    AttemptedAtUtc = DateTime.UtcNow,
                    SentAtUtc = sentAtUtc
                };
                _dbContext.EmailDeliveryLogs.Add(deliveryLog);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                var pendingLogs = _dbContext.ChangeTracker.Entries<EmailDeliveryLog>()
                    .Where(entry => entry.State == EntityState.Added).ToList();
                foreach (var pendingLog in pendingLogs)
                    pendingLog.State = EntityState.Detached;
                _logger.LogError(exception, "Could not persist notification email delivery log.");
            }
        }
    }
}