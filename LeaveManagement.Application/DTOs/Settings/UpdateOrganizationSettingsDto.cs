using Microsoft.AspNetCore.Http;

namespace LeaveManagement.Application.DTOs.Settings
{
    public class UpdateOrganizationSettingsDto
    {
        public string CompanyName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string CompanySize { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public IFormFile? CompanyLogo { get; set; } // Optional: only provided if changing logo
    }
}
