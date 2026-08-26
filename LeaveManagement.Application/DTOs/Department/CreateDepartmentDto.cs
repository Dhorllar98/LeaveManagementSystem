using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Application.DTOs.Department;

public class CreateDepartmentDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}