using DIBA_Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    public class EmailDeliveryLogsController : ControllerBase
    {
        private readonly DIBABookingsDbContext _dbContext;

        public EmailDeliveryLogsController(DIBABookingsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Return a bounded, newest-first view. Only Administrators may inspect
        // recipient addresses and provider error details.
        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var logs = await _dbContext.EmailDeliveryLogs
                .AsNoTracking()
                .OrderByDescending(log => log.AttemptedAtUtc)
                .Take(100)
                .Select(log => new
                {
                    log.EmailDeliveryLogId,
                    log.UserId,
                    log.NotificationId,
                    log.RecipientEmail,
                    log.Subject,
                    log.Status,
                    log.ErrorMessage,
                    log.AttemptedAtUtc,
                    log.SentAtUtc
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}
