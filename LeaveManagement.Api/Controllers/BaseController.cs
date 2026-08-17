using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Extracts the authenticated User ID from the JWT token claims.
    /// </summary>
    protected Guid GetCurrentUserId()
    {
        var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? HttpContext.User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Extracts the Organization ID directly from the JWT token claims (if included during login).
    /// </summary>
    protected Guid? GetCurrentOrganizationId()
    {
        var orgIdClaim = HttpContext.User.FindFirst("organizationId")?.Value
                         ?? HttpContext.User.FindFirst(ClaimTypes.GroupSid)?.Value;

        return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : null;
    }
}