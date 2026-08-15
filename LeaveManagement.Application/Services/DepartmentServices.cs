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

    // FIX: Require organizationId
    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentDto dto, Guid organizationId)
    {
        // FIX: Scope the duplicate name check to the specific organization
        if (await _departmentRepository.ExistsByNameAsync(dto.Name, organizationId))
        {
            throw new InvalidOperationException($"Department '{dto.Name}' already exists within your organization.");
        }

        if (dto.TeamLeadId.HasValue)
        {
            var teamLead = await _userRepository.GetByIdAsync(dto.TeamLeadId.Value);

            // FIX: Ensure the team lead actually belongs to this organization
            if (teamLead == null || teamLead.Role != UserRole.TeamLead || teamLead.OrganizationId != organizationId)
            {
                throw new InvalidOperationException("Assigned user must exist, have the TeamLead role, and belong to your organization.");
            }
        }

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            TeamLeadId = dto.TeamLeadId,
            OrganizationId = organizationId, // FIX: Lock the department to the tenant
            CreatedAt = DateTime.UtcNow
        };

        await _departmentRepository.AddAsync(department);
        await _departmentRepository.SaveChangesAsync();

        return await GetDepartmentByIdAsync(department.Id, organizationId)
               ?? throw new InvalidOperationException("Failed to retrieve created department.");
    }

    // FIX: Require organizationId
    public async Task<DepartmentDto> AssignTeamLeadAsync(AssignTeamLeadDto dto, Guid organizationId)
    {
        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId)
            ?? throw new KeyNotFoundException("Department not found.");

        // FIX: Ensure the department being modified belongs to the user's organization
        if (department.OrganizationId != organizationId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this department.");
        }

        var teamLead = await _userRepository.GetByIdAsync(dto.TeamLeadId)
            ?? throw new KeyNotFoundException("User not found.");

        // FIX: Ensure the team lead belongs to the user's organization
        if (teamLead.Role != UserRole.TeamLead || teamLead.OrganizationId != organizationId)
        {
            throw new InvalidOperationException("User must have the TeamLead role and belong to your organization.");
        }

        department.TeamLeadId = dto.TeamLeadId;
        department.UpdatedAt = DateTime.UtcNow;

        teamLead.DepartmentId = dto.DepartmentId;

        await _departmentRepository.UpdateAsync(department);
        await _departmentRepository.SaveChangesAsync();

        return await GetDepartmentByIdAsync(department.Id, organizationId)
               ?? throw new InvalidOperationException("Failed to retrieve updated department.");
    }

    // FIX: Require organizationId
    public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync(Guid organizationId)
    {
        // FIX: Fetch only departments linked to this specific organization
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

    // FIX: Require organizationId
    public async Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id, Guid organizationId)
    {
        var d = await _departmentRepository.GetByIdAsync(id);

        // FIX: Ensure the fetched department belongs to the user's organization
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