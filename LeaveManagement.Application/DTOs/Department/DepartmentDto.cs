namespace LeaveManagement.Application.DTOs.Department;

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? TeamLeadId { get; set; }
    public string? TeamLeadName { get; set; }
    public int EmployeeCount { get; set; }
    public List<DepartmentEmployeeDto> Employees { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class DepartmentEmployeeDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}