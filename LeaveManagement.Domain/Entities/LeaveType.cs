namespace LeaveManagement.Domain.Entities;

public class LeaveType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string Name { get; set; } = string.Empty;
    public int DefaultDays { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}