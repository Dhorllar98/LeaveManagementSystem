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

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto, Guid organizationId)
    {
        // Scope duplicate name check to the organization
        if (await _departmentRepository.ExistsByNameAsync(dto.Name, organizationId))
        {
            throw new InvalidOperationException($"Department '{dto.Name}' already exists within your organization.");
        }

        // Departments are ALWAYS created unassigned (TeamLeadId = null).
        // HR must explicitly call AssignTeamLeadAsync to assign a Team Lead.
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            TeamLeadId = null,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        };

        await _departmentRepository.AddAsync(department);
        await _departmentRepository.SaveChangesAsync();

        return await GetDepartmentByIdAsync(department.Id, organizationId)
               ?? throw new InvalidOperationException("Failed to retrieve created department.");
    }

    public async Task<DepartmentDto> AssignTeamLeadAsync(AssignTeamLeadDto dto, Guid organizationId)
    {
        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId)
            ?? throw new KeyNotFoundException("Department not found.");

        if (department.OrganizationId != organizationId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this department.");
        }

        var teamLead = await _userRepository.GetByIdAsync(dto.TeamLeadId)
            ?? throw new KeyNotFoundException("User not found.");

        if (teamLead.OrganizationId != organizationId)
        {
            throw new InvalidOperationException("Assigned user must belong to your organization.");
        }

        // Explicit HR Assignment: Update department team lead and assign employee to department
        department.TeamLeadId = dto.TeamLeadId;
        department.UpdatedAt = DateTime.UtcNow;

        teamLead.DepartmentId = dto.DepartmentId;

        // Auto-promote role to TeamLead if not already set
        if (teamLead.Role != UserRole.TeamLead)
        {
            teamLead.Role = UserRole.TeamLead;
            await _userRepository.UpdateAsync(teamLead);
        }

        await _departmentRepository.UpdateAsync(department);
        await _departmentRepository.SaveChangesAsync();

        return await GetDepartmentByIdAsync(department.Id, organizationId)
               ?? throw new InvalidOperationException("Failed to retrieve updated department.");
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync(Guid organizationId)
    {
        var departments = await _departmentRepository.GetAllByOrganizationAsync(organizationId);

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

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id, Guid organizationId)
    {
        var d = await _departmentRepository.GetByIdAsync(id);

        if (d == null || d.OrganizationId != organizationId) return null;

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