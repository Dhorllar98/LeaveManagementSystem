namespace LeaveManagement.Application.DTOs.User;

public class UpcomingBirthdayDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? Designation { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public int DaysUntilBirthday { get; set; }
    public bool IsToday { get; set; }
}