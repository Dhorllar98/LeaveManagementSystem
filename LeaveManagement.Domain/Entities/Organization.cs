namespace LeaveManagement.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // e.g. "SBSC UK", "SBSC Nigeria"
    public string Name { get; set; } = string.Empty;

    // e.g. "SBSC-UK", "SBSC-NIG"
    public string CodePrefix { get; set; } = string.Empty;

    public int LastEmployeeNumber { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<LeaveType> LeaveTypes { get; set; } = new List<LeaveType>();
}