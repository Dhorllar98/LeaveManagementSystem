using LeaveManagement.Application.DTOs.Department;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : BaseController
{
    private readonly IDepartmentService _departmentService;
    private readonly IUserRepository _userRepository;

    public DepartmentsController(IDepartmentService departmentService, IUserRepository userRepository)
    {
        _departmentService = departmentService;
        _userRepository = userRepository;
    }

    private async Task<Guid?> GetUserOrganizationIdAsync()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty) return null;

        var user = await _userRepository.GetByIdAsync(currentUserId);
        return user?.OrganizationId;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orgId = await GetUserOrganizationIdAsync();
        if (orgId == null) return BadRequest(new { message = "User organization not found." });

        var departments = await _departmentService.GetAllDepartmentsAsync(orgId.Value);
        return Ok(departments);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var orgId = await GetUserOrganizationIdAsync();
        if (orgId == null) return BadRequest(new { message = "User organization not found." });

        var department = await _departmentService.GetDepartmentByIdAsync(id, orgId.Value);
        if (department == null)
            return NotFound(new { message = $"Department with ID '{id}' not found." });

        return Ok(department);
    }

    [HttpPost]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
    {
        var orgId = await GetUserOrganizationIdAsync();
        if (orgId == null) return BadRequest(new { message = "User organization not found." });

        var department = await _departmentService.CreateDepartmentAsync(dto, orgId.Value);
        return CreatedAtAction(nameof(GetById), new { id = department.Id }, department);
    }

    [HttpPost("assign-team-lead")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> AssignTeamLead([FromBody] AssignTeamLeadDto dto)
    {
        var orgId = await GetUserOrganizationIdAsync();
        if (orgId == null) return BadRequest(new { message = "User organization not found." });

        var updatedDepartment = await _departmentService.AssignTeamLeadAsync(dto, orgId.Value);
        return Ok(new
        {
            success = true,
            message = "Team lead assigned successfully.",
            data = updatedDepartment
        });
    }
}