namespace LeaveManagement.Application.DTOs.Auth;

public record RegisterOrganizationDto(
    string CompanyName,
    string CodePrefix,      // e.g., "SBSC-UK"
    string AdminFullName,
    string AdminEmail,
    string Password
);