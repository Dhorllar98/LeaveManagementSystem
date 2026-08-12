using System.ComponentModel.DataAnnotations;

namespace LeaveManagement.Application.DTOs.Department;

public class AssignTeamLeadDto
{
    [Required]
    public Guid DepartmentId { get; set; }

    [Required]
    public Guid TeamLeadId { get; set; }
}