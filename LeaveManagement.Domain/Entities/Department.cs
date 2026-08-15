using LeaveManagement.Domain.Entities;

namespace LeaveManagement.Domain.Entities;

public class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Required for multi-tenancy isolation
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid? TeamLeadId { get; set; }
    public User? TeamLead { get; set; }

    public ICollection<User>? Employees { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}