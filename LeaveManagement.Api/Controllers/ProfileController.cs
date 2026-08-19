using LeaveManagement.Application.DTOs.Profile;
using LeaveManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : BaseController
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        var profile = await _profileService.GetProfileAsync(currentUserId, cancellationToken);
        if (profile == null) return NotFound(new { message = "Profile not found." });

        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return Unauthorized();

        var (success, message, statusCode) = await _profileService.UpdateProfileAsync(currentUserId, dto, cancellationToken);

        if (!success)
        {
            return statusCode switch
            {
                401 => Unauthorized(new { message }),
                403 => StatusCode(StatusCodes.Status403Forbidden, new { message }),
                404 => NotFound(new { message }),
                _ => BadRequest(new { message })
            };
        }

        return Ok(new
        {
            success = true,
            message
        });
    }
}