using System;
using System.Security.Claims;
using LeaveManagement.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LeaveManagement.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var userIdStr = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userIdStr, out var userId))
        {
            UserId = userId;
        }
    }

    public Guid? UserId { get; }
}