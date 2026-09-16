using DIBA_Backend.Data;
using DIBA_Backend.Dto.AuditLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Staff")]
    public class AuditLogsController : ControllerBase
    {
        private readonly DIBABookingsDbContext dbContext;

        public AuditLogsController(DIBABookingsDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // GET: api/AuditLogs
        [HttpGet]
        public async Task<IActionResult> GetAuditLogs()
        {
            var auditLogs = await dbContext.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new AuditLogResponseDto
                {
                    AuditLogId = a.AuditLogId,
                    Action = a.Action,
                    LogDescription = a.LogDescription,
                    Timestamp = a.Timestamp,
                    UserId = a.UserId,
                    UserName = a.User == null ? string.Empty : a.User.FirstName + " " + a.User.LastName
                })
                .ToListAsync();

            return Ok(auditLogs);
        }

        // GET: api/AuditLogs/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetAuditLog(Guid id)
        {
            var auditLog = await dbContext.AuditLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AuditLogId == id);

            if (auditLog == null)
            {
                return NotFound("Audit log not found.");
            }

            var response = new AuditLogResponseDto
            {
                AuditLogId = auditLog.AuditLogId,
                Action = auditLog.Action,
                LogDescription = auditLog.LogDescription,
                Timestamp = auditLog.Timestamp,
                UserId = auditLog.UserId,
                UserName = auditLog.User == null ? string.Empty : auditLog.User.FirstName + " " + auditLog.User.LastName
            };

            return Ok(response);
        }
    }
}
