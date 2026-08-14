using LeaveManagement.Application.DTOs.Department;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Domain.Interfaces;

namespace LeaveManagement.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;

    public DepartmentService(IDepartmentRepository departmentRepository, IUserRepository userRepository)
    {
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto)
    {
        if (await _departmentRepository.ExistsByNameAsync(dto.Name))
        {
            throw new InvalidOperationException($"Department '{dto.Name}' already exists.");
        }

        if (dto.TeamLeadId.HasValue)
        {
            var teamLead = await _userRepository.GetByIdAsync(dto.TeamLeadId.Value);
            if (teamLead == null || teamLead.Role != UserRole.TeamLead)
            {
                throw new InvalidOperationException("Assigned user must exist and have the TeamLead role.");
            }
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            TeamLeadId = dto.TeamLeadId,
            CreatedAt = DateTime.UtcNow
        };

        await _departmentRepository.AddAsync(department);
        await _departmentRepository.SaveChangesAsync();

        return await GetDepartmentByIdAsync(department.Id)
               ?? throw new InvalidOperationException("Failed to retrieve created department.");
    }

    public async Task<DepartmentDto> AssignTeamLeadAsync(AssignTeamLeadDto dto)
    {
        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId)
            ?? throw new KeyNotFoundException("Department not found.");

        var teamLead = await _userRepository.GetByIdAsync(dto.TeamLeadId)
            ?? throw new KeyNotFoundException("User not found.");

        if (teamLead.Role != UserRole.TeamLead)
        {
            throw new InvalidOperationException("User must have the TeamLead role.");
        }

        department.TeamLeadId = dto.TeamLeadId;
        department.UpdatedAt = DateTime.UtcNow;

        teamLead.DepartmentId = dto.DepartmentId;

        await _departmentRepository.UpdateAsync(department);
        await _departmentRepository.SaveChangesAsync();

        return await GetDepartmentByIdAsync(department.Id)
               ?? throw new InvalidOperationException("Failed to retrieve updated department.");
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync()
    {
        var departments = await _departmentRepository.GetAllAsync();

        return departments.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            TeamLeadId = d.TeamLeadId,
            TeamLeadName = d.TeamLead?.FullName ?? "Unassigned",
            EmployeeCount = d.Employees?.Count ?? 0,
            Employees = d.Employees?.Select(e => new DepartmentEmployeeDto
            {
                Id = e.Id,
                FullName = e.FullName,
                Email = e.Email,
                Designation = e.Designation ?? string.Empty,
                Role = e.Role.ToString()
            }).ToList() ?? new List<DepartmentEmployeeDto>(),
            CreatedAt = d.CreatedAt
        });
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id)
    {
        var d = await _departmentRepository.GetByIdAsync(id);
        if (d == null) return null;

        return new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            TeamLeadId = d.TeamLeadId,
            TeamLeadName = d.TeamLead?.FullName ?? "Unassigned",
            EmployeeCount = d.Employees?.Count ?? 0,
            Employees = d.Employees?.Select(e => new DepartmentEmployeeDto
            {
                Id = e.Id,
                FullName = e.FullName,
                Email = e.Email,
                Designation = e.Designation ?? string.Empty,
                Role = e.Role.ToString()
            }).ToList() ?? new List<DepartmentEmployeeDto>(),
            CreatedAt = d.CreatedAt
        };
    }
}