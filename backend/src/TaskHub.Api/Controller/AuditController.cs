using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Task_hub.Application.Abstractions;
using Task_hub.Application.Authorization;
using Task_hub.Application.Dto;
using Task_hub.Application.Extensions;
using TaskHub.Core.Entities;

namespace TaskHub.Api.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class AuditController : ControllerBase
    {
        private readonly IStorage _storage;
        private readonly IOrganisationContext _organisationContext;
        private readonly ILogger<AuditController> _logger;

        public AuditController(
          IStorage storage,
          IOrganisationContext organisationContext,
          ILogger<AuditController> logger)
        {
            _storage = storage;
            _organisationContext = organisationContext;
            _logger = logger;
        }

        [HttpGet]
        [RequireOrganisation(RequireAdmin = true)]
        [ProducesResponseType(typeof(AuditLogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AuditLogResponse>> GetAuditLogs([FromQuery] AuditQuery query)
        {
            var orgId = _organisationContext.CurrentOrganisationId!.Value;

            var logs = await _storage.GetAuditLogsAsync(
                orgId,
                query.From,
                query.To);

            // Apply pagination
            var totalCount = logs.Count();
            var paginatedLogs = logs
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(l => l.ToResponse());

            Response.Headers.Append("X-Total-Count", totalCount.ToString());

            return Ok(new AuditLogResponse
            {
                Logs = paginatedLogs,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize
            });
        }

        [HttpGet("summary")]
        [RequireOrganisation(RequireAdmin = true)]
        [ProducesResponseType(typeof(AuditSummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<AuditSummaryResponse>> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var orgId = _organisationContext.CurrentOrganisationId!.Value;
            var logs = await _storage.GetAuditLogsAsync(orgId, from, to);

            var summary = logs
                .GroupBy(l => l.ActionType)
                .Select(g => new ActionTypeSummary
                {
                    ActionType = g.Key,
                    Count = g.Count(),
                    LastOccurrence = g.Max(l => l.Timestamp)
                });

            return Ok(new AuditSummaryResponse
            {
                From = from ?? DateTime.UtcNow.AddDays(-30),
                To = to ?? DateTime.UtcNow,
                Actions = summary
            });
        }
    }
}
