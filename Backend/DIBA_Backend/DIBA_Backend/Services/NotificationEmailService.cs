using System.Net;
using System.Net.Mail;
using DIBA_Backend.Data;
using DIBA_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Services
{
    /// <summary>
    /// Sends notification emails when SMTP is configured and records Sent,
    /// Failed, or Skipped outcomes. Email failure never undoes a saved booking.
    /// </summary>
    public sealed class NotificationEmailService
    {
        private readonly DIBABookingsDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationEmailService> _logger;

        public NotificationEmailService(
            DIBABookingsDbContext dbContext,
            IConfiguration configuration,
            ILogger<NotificationEmailService> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task TrySendAsync(
            Guid userId,
            Guid? notificationId,
            string subject,
            string body)
        {
            var recipient = await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.UserId == userId)
                .Select(user => user.Email)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(recipient))
            {
                await RecordOutcomeAsync(userId, notificationId, string.Empty, subject,
                    "Failed", "The notification recipient has no email address.", null);
                _logger.LogWarning("Notification email skipped because user {UserId} has no email address.", userId);
                return;
            }

            var enabled = _configuration.GetValue<bool>("Email:Enabled");
            var host = _configuration["Email:SmtpHost"];
            var fromAddress = _configuration["Email:FromAddress"];

            if (!enabled || string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
            {
                await RecordOutcomeAsync(userId, notificationId, recipient, subject,
                    "Skipped", "Email delivery is disabled or SMTP is not configured.", null);
                _logger.LogInformation("Email delivery is not configured; notification {NotificationId} remains in-app only.", notificationId);
                return;
            }

            var port = _configuration.GetValue<int?>("Email:Port") ?? 587;
            var enableSsl = _configuration.GetValue<bool?>("Email:EnableSsl") ?? true;
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    UseDefaultCredentials = false,
                    Credentials = string.IsNullOrWhiteSpace(username)
                        ? CredentialCache.DefaultNetworkCredentials
                        : new NetworkCredential(username, password)
                };

                using var message = new MailMessage(fromAddress, recipient, subject, body);
                await client.SendMailAsync(message);

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
                _dbContext.EmailDeliveryLogs.Add(new EmailDeliveryLog
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
                });

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                // Logging failure must not break booking/payment processing.
                _logger.LogError(exception, "Could not persist notification email delivery log.");
            }
        }
    }
}
