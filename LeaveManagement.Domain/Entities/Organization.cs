namespace LeaveManagement.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string? CompanySize { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }

    public string CodePrefix { get; set; } = string.Empty;
    public int LastEmployeeNumber { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<LeaveType> LeaveTypes { get; set; } = new List<LeaveType>();
}