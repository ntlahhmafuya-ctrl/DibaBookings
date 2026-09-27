using DIBA_Backend.Data;
using DIBA_Backend.Dto.Notification;
using DIBA_Backend.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
    // Similar logic: requiring an authenticated user before accessing
    // protected controller actions.
    // DIBA adaptation: all notification operations require the user
    // to be logged in.
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly DIBABookingsDbContext _dbContext;

        public NotificationsController(DIBABookingsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: api/Notifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (!UserHelper.TryGetUserId(User, out Guid userId))
            {
                return Unauthorized("User ID could not be determined.");
            }

            // Reference: Oh011, "NotificationService".
            // Similar logic: retrieving notifications belonging to
            // the currently authenticated user rather than returning
            // notifications belonging to other users.
            // DIBA adaptation: the UserId from the authenticated user's
            // claims is used to restrict the notification query.
            var notifications = await _dbContext.Notifications
                .Where(notification => notification.UserId == userId)

                // Similar logic: displaying the most recent notifications
                // first. This is a standard notification-inbox approach.
                .OrderByDescending(notification => notification.DateCreated)

                // Similar logic: returning notification data through a
                // response model instead of exposing the database entity
                // directly.
                // DIBA adaptation: NotificationResponseDto contains only
                // the fields required by the frontend.
                .Select(notification => new NotificationResponseDto
                {
                    NotificationId = notification.NotificationId,
                    NotificationType = notification.NotificationType,
                    Message = notification.Message,
                    DateCreated = notification.DateCreated,
                    IsRead = notification.IsRead,
                    UserId = notification.UserId,
                    BookingId = notification.BookingId
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // GET: api/Notifications/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetNotification(Guid id)
        {
            if (!UserHelper.TryGetUserId(User, out Guid userId))
            {
                return Unauthorized("User ID could not be determined.");
            }

            // Reference: Oh011, "NotificationService".
            // Similar logic: retrieving a notification for a specific
            // authenticated user.
            // DIBA adaptation: both the notification ID and UserId are
            // checked, preventing a user from retrieving another user's
            // notification by changing the ID in the request.
            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(notification =>
                    notification.NotificationId == id &&
                    notification.UserId == userId);

            if (notification == null)
            {
                return NotFound("Notification not found.");
            }

            var response = new NotificationResponseDto
            {
                NotificationId = notification.NotificationId,
                NotificationType = notification.NotificationType,
                Message = notification.Message,
                DateCreated = notification.DateCreated,
                IsRead = notification.IsRead,
                UserId = notification.UserId,
                BookingId = notification.BookingId
            };

            return Ok(response);
        }

        // PUT: api/Notifications/{id}/read
        [HttpPut("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            // Reference: Microsoft Learn, "Claims-based authorization".
            // Similar logic: obtaining the authenticated user's identity
            // from a claim attached to the current request.
            // DIBA adaptation: the NameIdentifier claim contains the
            // logged-in user's UserId.
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized("User ID could not be determined.");
            }

            if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            // Reference: Oh011, "NotificationService".
            // Similar logic: modifying a notification belonging to the
            // authenticated user.
            // DIBA adaptation: the notification ID and UserId are both
            // checked so that users can only modify their own notifications.
            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(notification =>
                    notification.NotificationId == id &&
                    notification.UserId == userId);

            if (notification == null)
            {
                return NotFound("Notification not found.");
            }

            // Similar logic: changing the read/unread state of a
            // notification.
            notification.IsRead = true;

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Notification marked as read.",
                notificationId = notification.NotificationId
            });
        }

        // PUT: api/Notifications/read-all
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            // Reference: Microsoft Learn, "Claims-based authorization".
            // Similar logic: obtaining the authenticated user's identity
            // from the current user's claims.
            // DIBA adaptation: the claim is converted into the Guid used
            // by the database User entity.
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized("User ID could not be determined.");
            }

            if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            // Reference: Oh011, "NotificationService".
            // Similar logic: selecting unread notifications belonging to
            // the current user before changing their read state.
            // DIBA adaptation: the query specifically checks both UserId
            // and IsRead so that already-read notifications are not updated.
            var notifications = await _dbContext.Notifications
                .Where(notification =>
                    notification.UserId == userId &&
                    !notification.IsRead)
                .ToListAsync();

            // Similar logic: updating the read state of multiple
            // notifications in one operation.
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "All notifications marked as read.",
                count = notifications.Count
            });
        }
    }
}