namespace LeaveManagement.Application.DTOs.Users;

public class UserFilterDto
{
    public string? SearchTerm { get; set; } // Case-insensitive search across FullName, Email, and EmployeeCode.


    public Guid? DepartmentId { get; set; } // Filter by specific department ID.


    public string? Role { get; set; } // Filter by Role (e.g., "HR", "Employee", "Manager").

    
    public bool? IsActive { get; set; } // Filter active/inactive users.

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}