using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using DIBA_Backend.Data;
using DIBA_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Services
{
    /// <summary>
    /// Sends notification emails through Microsoft Graph OAuth or configured SMTP
    /// and records Sent, Failed, or Skipped outcomes. Email failure never undoes
    /// a saved booking.
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

            var enabled = _configuration.GetValue<bool>("Email:Enabled");
            if (!enabled)
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
                if (string.Equals(provider, "MicrosoftGraph", StringComparison.OrdinalIgnoreCase))
                {
                    var tenantId = _configuration["Email:MicrosoftGraph:TenantId"];
                    var clientId = _configuration["Email:MicrosoftGraph:ClientId"];
                    var clientSecret = _configuration["Email:MicrosoftGraph:ClientSecret"];

                    if (string.IsNullOrWhiteSpace(fromAddress) ||
                        string.IsNullOrWhiteSpace(tenantId) ||
                        string.IsNullOrWhiteSpace(clientId) ||
                        string.IsNullOrWhiteSpace(clientSecret))
                    {
                        await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                            "Skipped", "Microsoft Graph email settings are incomplete.", null);
                        _logger.LogWarning("Microsoft Graph email settings are incomplete; notification {NotificationId} was not sent.", notificationId);
                        return;
                    }

                    await SendWithMicrosoftGraphAsync(
                        tenantId, clientId, clientSecret, fromAddress, recipient, subject, body);
                }
                else
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

                await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                    "Sent", null, DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Notification email delivery failed for notification {NotificationId}.", notificationId);
                await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                    "Failed", $"{exception.GetType().Name}: {exception.Message}", null);
            }
        }

        private async Task SendWithMicrosoftGraphAsync(
            string tenantId,
            string clientId,
            string clientSecret,
            string fromAddress,
            string recipient,
            string subject,
            string body)
        {
            var httpClient = _httpClientFactory.CreateClient("MicrosoftGraph");

            using var tokenRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenantId)}/oauth2/v2.0/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["scope"] = "https://graph.microsoft.com/.default",
                    ["grant_type"] = "client_credentials"
                })
            };

            using var tokenResponse = await httpClient.SendAsync(tokenRequest);
            tokenResponse.EnsureSuccessStatusCode();

            using var tokenStream = await tokenResponse.Content.ReadAsStreamAsync();
            using var tokenJson = await JsonDocument.ParseAsync(tokenStream);
            if (!tokenJson.RootElement.TryGetProperty("access_token", out var accessTokenElement) ||
                string.IsNullOrWhiteSpace(accessTokenElement.GetString()))
            {
                throw new InvalidOperationException(
                    "Microsoft identity platform did not return an access token.");
            }

            var accessToken = accessTokenElement.GetString()!;
            var emailPayload = new
            {
                message = new
                {
                    subject,
                    body = new
                    {
                        contentType = "Text",
                        content = body
                    },
                    toRecipients = new[]
                    {
                        new
                        {
                            emailAddress = new
                            {
                                address = recipient
                            }
                        }
                    }
                },
                saveToSentItems = true
            };

            using var sendRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(fromAddress)}/sendMail")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(emailPayload),
                    Encoding.UTF8,
                    "application/json")
            };
            sendRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            using var sendResponse = await httpClient.SendAsync(sendRequest);
            sendResponse.EnsureSuccessStatusCode();
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
                // Detach a failed log insert so it cannot break a later
                // SaveChanges call in the same booking/payment request.
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
