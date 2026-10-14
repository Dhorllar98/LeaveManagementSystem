namespace LeaveManagement.Domain.Entities;

public class EmployeeDocument
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string DocumentName { get; set; } = string.Empty; // e.g., "ID Card", "Contract Renewal", "Degree Certificate"
    public string DocumentType { get; set; } = string.Empty; // e.g., "Credential", "Contract", "Other"
    public string FilePath { get; set; } = string.Empty;     // Storage path or URL
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}