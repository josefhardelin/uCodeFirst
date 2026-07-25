using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using uCodeFirst.Configuration;
using uCodeFirst.Sync;
using Umbraco.Cms.Api.Common.Builders;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;

namespace uCodeFirst.Api;

// Backend for the "backoffice dry-run dashboard" (see wwwroot/App_Plugins/uCodeFirst). Exposes the
// live CodeFirstSyncService.ComputePlanAsync() result over the Umbraco 17 Management API so the Lit
// dashboard can show creates/updates/prunes without waiting for (or restarting) startup.
//
// ManagementApiControllerBase already carries [Authorize(Policy = "BackOfficeAccess")] and
// [Authorize(Policy = "UmbracoFeatureEnabled")] — inheriting it is what makes this endpoint
// backoffice-authenticated; no extra [Authorize] is needed here.
//
// This controller must be `public` for ASP.NET Core's ControllerFeatureProvider to discover it
// (it explicitly requires TypeInfo.IsPublic — see Microsoft.AspNetCore.Mvc.Core). CodeFirstSyncService
// and CodeFirstOptions stay `internal` per this codebase's convention; a public constructor or action
// parameter of an internal type would fail to compile (CS0051 — inconsistent accessibility), so both
// are resolved from HttpContext.RequestServices inside the action body instead of via constructor/
// parameter injection.
[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("code-first")]
[ApiExplorerSettings(GroupName = "Code First")]
public class PlanCodeFirstController : ManagementApiControllerBase
{
    [HttpGet("plan")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Plan(CancellationToken cancellationToken)
    {
        var syncService = HttpContext.RequestServices.GetRequiredService<CodeFirstSyncService>();
        var options = HttpContext.RequestServices.GetRequiredService<IOptions<CodeFirstOptions>>().Value;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        try
        {
            var result = await syncService.ComputePlanAsync(assemblies, options.Strategy, options.Enabled, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            // Pre-flight validation failed (duplicate alias/GUID, dangling reference, etc.) — surface it
            // as a 400 rather than a 500 so the dashboard can render it as "plan unavailable", the same
            // failure CodeFirstStartupHandler would otherwise only report via a startup log warning.
            return BadRequest(new ProblemDetailsBuilder()
                .WithTitle("uCodeFirst pre-flight validation failed")
                .WithDetail(ex.Message)
                .Build());
        }
    }
}
