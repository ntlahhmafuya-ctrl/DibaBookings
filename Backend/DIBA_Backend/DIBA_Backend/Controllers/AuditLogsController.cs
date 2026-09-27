using DIBA_Backend.Data;
using DIBA_Backend.Dto.AuditLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    // Reference: Microsoft Learn, "Role-based authorization in ASP.NET Core".
    // Similar logic: restricting access to a controller by specifying
    // the roles that are permitted to access it.
    // DIBA adaptation: only Administrators and Staff can access audit logs
    // because audit information is intended for authorised personnel.
    // https://learn.microsoft.com/aspnet/core/security/authorization/roles
    [Authorize(Roles = "Administrator,Staff")]
    public class AuditLogsController : ControllerBase
    {
        private readonly DIBABookingsDbContext _dbContext;

        public AuditLogsController(
            DIBABookingsDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        // GET: api/AuditLogs
        [HttpGet]
        public async Task<IActionResult> GetAuditLogs()
        {
            // Reference: anassrh, "Secure Backend API with ASP.NET Core".
            // Similar logic: audit records are stored and made available
            // through protected audit-log endpoints.
            //
            // DIBA adaptation: the AuditLogs table is queried directly
            // through Entity Framework Core and only authorised Staff
            // and Administrators can access this endpoint.
            // https://github.com/anassrh/SecureHexagonalApi
            var auditLogs = await _dbContext.AuditLogs


                // Similar Entity Framework Core relationship-loading
                // pattern: retrieve related user information together with
                // the audit record.
                //
                // DIBA adaptation: the User relationship is required so
                // the audit-log response can display the name of the user
                // who performed the action.
                .Include(auditLog =>
                    auditLog.User)


                // DIBA reporting requirement:
                // the newest audit records are displayed first.
                //
                // The use of OrderByDescending with a timestamp is a
                // standard data-retrieval pattern for displaying recent
                // records first.
                .OrderByDescending(auditLog =>
                    auditLog.Timestamp)


                // Similar DTO projection pattern used in ASP.NET Core
                // Entity Framework applications.
                //
                // DIBA adaptation: the database entity is projected into
                // AuditLogResponseDto instead of returning the complete
                // AuditLog entity directly.
                .Select(auditLog =>
                    new AuditLogResponseDto
                    {
                        AuditLogId =
                            auditLog.AuditLogId,

                        Action =
                            auditLog.Action,

                        LogDescription =
                            auditLog.LogDescription,

                        Timestamp =
                            auditLog.Timestamp,

                        UserId =
                            auditLog.UserId,

                        // Similar null-handling pattern:
                        // if the related User record is unavailable,
                        // an empty string is returned instead of causing
                        // a null-reference error.
                        UserName =
                            auditLog.User == null
                                ? string.Empty
                                : auditLog.User.FirstName +
                                  " " +
                                  auditLog.User.LastName
                    })
                .ToListAsync();

            return Ok(auditLogs);
        }


        // GET: api/AuditLogs/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetAuditLog(Guid id)
        {
            // Similar Entity Framework Core lookup pattern:
            // retrieve one record using its unique identifier and include
            // the related user information.
            //
            // DIBA adaptation:
            // the AuditLogId is used to retrieve a specific audit record
            // and the User relationship is included for display purposes.
            var auditLog = await _dbContext.AuditLogs
                .Include(auditLog =>
                    auditLog.User)
                .FirstOrDefaultAsync(
                    auditLog =>
                        auditLog.AuditLogId == id);

            if (auditLog == null)
            {
                return NotFound(
                    "Audit log not found.");
            }


            // Similar DTO projection/mapping pattern:
            // database entities are converted into response objects
            // before being returned to the client.
            //
            // DIBA adaptation:
            // only the audit information required by the frontend is
            // returned.
            var response = new AuditLogResponseDto
            {
                AuditLogId =
                    auditLog.AuditLogId,

                Action =
                    auditLog.Action,

                LogDescription =
                    auditLog.LogDescription,

                Timestamp =
                    auditLog.Timestamp,

                UserId =
                    auditLog.UserId,

                UserName =
                    auditLog.User == null
                        ? string.Empty
                        : auditLog.User.FirstName +
                          " " +
                          auditLog.User.LastName
            };

            return Ok(response);
        }
    }
}