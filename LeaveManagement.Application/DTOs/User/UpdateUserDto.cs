namespace LeaveManagement.Application.DTOs.User;

public class UpdateUserDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? TeamLeadId { get; set; }
    public string? Designation { get; set; }
    public string? Role { get; set; }
    public int? LeaveBalance { get; set; }
}