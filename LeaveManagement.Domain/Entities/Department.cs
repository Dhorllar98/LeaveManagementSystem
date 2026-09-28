using System;
using System.Collections.Generic;

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

    // Initialized to prevent null reference issues
    public ICollection<User> Employees { get; set; } = new List<User>();

    // Audit Properties
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}