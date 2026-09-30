using BookingSystem.Application.Authorization;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize(Policy = AccessPolicies.OnlyAdmin)]
[Route("api/integrations")]
public class IntegrationsController : ControllerBase
{
    private readonly IExternalIntegrationGateway _integrationGateway;
    private readonly AuditLogWriter _auditLogWriter;

    public IntegrationsController(IExternalIntegrationGateway integrationGateway, AuditLogWriter auditLogWriter)
    {
        _integrationGateway = integrationGateway;
        _auditLogWriter = auditLogWriter;
    }

    [HttpGet("readiness")]
    public async Task<ActionResult<IntegrationReadinessDto>> GetReadiness(CancellationToken cancellationToken)
    {
        var readiness = await _integrationGateway.GetReadinessAsync(cancellationToken);
        await _auditLogWriter.RecordReadAsync("IntegrationReadiness", readiness.RegionCode, cancellationToken);
        return Ok(readiness);
    }
}
