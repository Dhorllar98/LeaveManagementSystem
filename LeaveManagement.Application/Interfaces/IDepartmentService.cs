using LeaveManagement.Application.DTOs.Department;

namespace LeaveManagement.Application.Interfaces;

public interface IDepartmentService
{
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto);
    Task<DepartmentDto> AssignTeamLeadAsync(AssignTeamLeadDto dto);
    Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync();
    Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id);
}