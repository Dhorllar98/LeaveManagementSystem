using LeaveManagement.Application.DTOs.Department;
using LeaveManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[Authorize]
public class DepartmentsController : BaseController
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _departmentService.GetAllDepartmentsAsync();
        return Ok(departments);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var department = await _departmentService.GetDepartmentByIdAsync(id);
        if (department == null)
        {
            return NotFound(new { message = $"Department with ID '{id}' not found." });
        }

        return Ok(department);
    }

    //(HR Only)
    [HttpPost]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
    {
        var department = await _departmentService.CreateDepartmentAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = department.Id }, department);
    }

    // (HR Only)
    [HttpPost("assign-team-lead")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> AssignTeamLead([FromBody] AssignTeamLeadDto dto)
    {
        var updatedDepartment = await _departmentService.AssignTeamLeadAsync(dto);
        return Ok(new
        {
            success = true,
            message = "Team lead assigned successfully.",
            data = updatedDepartment
        });
    }
}