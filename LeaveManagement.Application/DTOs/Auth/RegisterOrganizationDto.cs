using Microsoft.AspNetCore.Http;

namespace LeaveManagement.Application.DTOs.Auth;

public record RegisterOrganizationDto(
    // Company Profile
    string CompanyName,
    string Industry,
    string? CompanySize,
    string? Website,
    IFormFile CompanyLogo, // (Mandatory)

    // HR Administrator Setup
    string AdminFullName,
    string AdminEmail,
    string? PhoneNumber,
    string? JobTitle,
    string Password,

    // System Preferences
    string CodePrefix
);