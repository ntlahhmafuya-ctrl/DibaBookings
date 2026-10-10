using System.Security.Claims;
using DIBA_Backend.Data;
using DIBA_Backend.Dto.PrivacyRequests;
using DIBA_Backend.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DIBA_Backend.Controllers
{
    /// <summary>
    /// PRIVACY REQUEST WORKFLOW
    /// Responsibility: let users submit and track privacy requests, and let Administrators review and respond.
    /// This workflow records requests only. It never automatically deletes accounts, bookings, or financial records.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PrivacyRequestsController : ControllerBase
    {
        private readonly DIBABookingsDbContext _dbContext;

        private static readonly string[] AllowedRequestTypes =
        {
            "Access", "Correction", "Deletion", "Objection", "Other"
        };

        private static readonly string[] AllowedStatuses =
        {
            "Submitted", "In Review", "Need More Information", "Resolved", "Rejected"
        };

        public PrivacyRequestsController(DIBABookingsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ============================================================
        // FUNCTION: Create
        // RESPONSIBILITY: create a privacy request for the authenticated user.
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Create(CreatePrivacyRequestDto input)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var requestType = input.RequestType?.Trim();
            var description = input.Description?.Trim();

            if (string.IsNullOrWhiteSpace(requestType) ||
                !AllowedRequestTypes.Contains(requestType, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Choose Access, Correction, Deletion, Objection, or Other.");
            }

            if (string.IsNullOrWhiteSpace(description) || description.Length > 2000)
            {
                return BadRequest("Please explain your request in 1–2000 characters.");
            }

            // The requester identity comes from the validated token, never from client-supplied UserId.
            var privacyRequest = new PrivacyRequest
            {
                UserId = userId.Value,
                RequestType = AllowedRequestTypes.First(type =>
                    type.Equals(requestType, StringComparison.OrdinalIgnoreCase)),
                Description = description,
                Status = "Submitted",
                SubmittedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _dbContext.Set<PrivacyRequest>().Add(privacyRequest);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMine),
                new { },
                new
                {
                    privacyRequest.PrivacyRequestId,
                    privacyRequest.RequestType,
                    privacyRequest.Description,
                    privacyRequest.Status,
                    privacyRequest.SubmittedAtUtc,
                    privacyRequest.UpdatedAtUtc,
                    privacyRequest.Response
                });
        }

        // ============================================================
        // FUNCTION: GetMine
        // RESPONSIBILITY: return only the signed-in user's own privacy requests.
        // ============================================================
        [HttpGet("my")]
        public async Task<IActionResult> GetMine()
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var requests = await _dbContext.Set<PrivacyRequest>()
                .AsNoTracking()
                .Where(item => item.UserId == userId.Value)
                .OrderByDescending(item => item.SubmittedAtUtc)
                .Select(item => new
                {
                    item.PrivacyRequestId,
                    item.RequestType,
                    item.Description,
                    item.Status,
                    item.SubmittedAtUtc,
                    item.UpdatedAtUtc,
                    item.Response
                })
                .ToListAsync();

            return Ok(requests);
        }

        // ============================================================
        // FUNCTION: GetAll
        // RESPONSIBILITY: give Administrators a queue of privacy requests to review.
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAll()
        {
            var requests = await _dbContext.Set<PrivacyRequest>()
                .AsNoTracking()
                .OrderBy(item => item.Status == "Submitted" ? 0 : 1)
                .ThenBy(item => item.SubmittedAtUtc)
                .Select(item => new
                {
                    item.PrivacyRequestId,
                    item.UserId,
                    RequesterName = item.User == null
                        ? "Unknown user"
                        : item.User.FirstName + " " + item.User.LastName,
                    RequesterEmail = item.User == null ? "" : item.User.Email,
                    item.RequestType,
                    item.Description,
                    item.Status,
                    item.SubmittedAtUtc,
                    item.UpdatedAtUtc,
                    item.Response,
                    item.ReviewedByUserId
                })
                .ToListAsync();

            return Ok(requests);
        }

        // ============================================================
        // FUNCTION: UpdateStatus
        // RESPONSIBILITY: let an Administrator update a request status and record a response.
        // ============================================================
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> UpdateStatus(Guid id, UpdatePrivacyRequestDto input)
        {
            var administratorId = GetAuthenticatedUserId();
            if (administratorId == null)
            {
                return Unauthorized();
            }

            var status = input.Status?.Trim();
            if (string.IsNullOrWhiteSpace(status) ||
                !AllowedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Choose Submitted, In Review, Need More Information, Resolved, or Rejected.");
            }

            var response = input.Response?.Trim();
            if (response?.Length > 2000)
            {
                return BadRequest("The response must not exceed 2000 characters.");
            }

            var privacyRequest = await _dbContext.Set<PrivacyRequest>()
                .FirstOrDefaultAsync(item => item.PrivacyRequestId == id);

            if (privacyRequest == null)
            {
                return NotFound("Privacy request not found.");
            }

            privacyRequest.Status = AllowedStatuses.First(value =>
                value.Equals(status, StringComparison.OrdinalIgnoreCase));
            privacyRequest.Response = string.IsNullOrWhiteSpace(response) ? null : response;
            privacyRequest.UpdatedAtUtc = DateTime.UtcNow;
            privacyRequest.ReviewedByUserId = administratorId.Value;

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                privacyRequest.PrivacyRequestId,
                privacyRequest.Status,
                privacyRequest.UpdatedAtUtc,
                privacyRequest.Response
            });
        }

        // ============================================================
        // HELPER: GetAuthenticatedUserId
        // RESPONSIBILITY: safely read the current account ID from the validated JWT claims.
        // ============================================================
        private Guid? GetAuthenticatedUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var parsedUserId)
                ? parsedUserId
                : null;
        }
    }
}