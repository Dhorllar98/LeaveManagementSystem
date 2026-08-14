namespace LeaveManagement.Domain.Entities;

public class NotificationSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    // Toggle: Email HR & Team Lead when an employee submits a new request
    public bool EnableNewLeaveRequestEmails { get; set; } = true;

    // Toggle: Email Employee when their leave request is Approved or Rejected
    public bool EnableLeaveStatusUpdateEmails { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // <-- Add this property
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}