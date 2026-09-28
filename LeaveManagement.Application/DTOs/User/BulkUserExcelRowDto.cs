namespace LeaveManagement.Application.DTOs.User;

public class BulkUserExcelRowDto
{
    public int RowNumber { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee";
    public string DepartmentName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string? TeamLeadEmail { get; set; }
    public DateOnly? DateOfBirth { get; set; }
}