using LeaveManagement.Application.DTOs.Department;

namespace LeaveManagement.Application.Interfaces;

public interface IDepartmentService
{
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto, Guid organizationId);
    Task<DepartmentDto> AssignTeamLeadAsync(AssignTeamLeadDto dto, Guid organizationId);
    Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync(Guid organizationId);
    Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id, Guid organizationId);
}