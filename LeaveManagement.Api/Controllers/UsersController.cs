using LeaveManagement.Application.DTOs.User;
using LeaveManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : BaseController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> GetUsers([FromQuery] UserFilterDto filter, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _userService.GetUsersAsync(currentUserId, filter, cancellationToken);

        if (result == null)
        {
            return BadRequest(new { message = "User organization not found." });
        }

        return Ok(result);
    }

    [HttpGet("department-colleagues")]
    public async Task<IActionResult> GetDepartmentColleagues(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _userService.GetDepartmentColleaguesAsync(currentUserId, cancellationToken);
        return Ok(new
        {
            success = true,
            message = "Department colleagues retrieved successfully.",
            data = result
        });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "HR,TeamLead")]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var user = await _userService.GetUserByIdAsync(id, currentUserId, cancellationToken);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID '{id}' not found or does not belong to your organization." });
        }

        return Ok(user);
    }

    [HttpPost("provision")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> ProvisionUser([FromBody] ProvisionUserDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            return Unauthorized(new { message = "User identity invalid." });
        }

        var result = await _userService.ProvisionUserAsync(currentUserId, dto, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _userService.UpdateUserAsync(id, currentUserId, dto, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("bulk-upload")]
    [Authorize(Roles = "HR")]
    public async Task<IActionResult> BulkUploadUsers(IFormFile file, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            return Unauthorized(new { message = "User identity invalid." });
        }

        var result = await _userService.BulkUploadUsersAsync(currentUserId, file, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.Message });
        }

        return StatusCode(result.StatusCode, result.Data);
    }

    [HttpGet("bulk-upload/template")]
    [Authorize(Roles = "HR")]
    public IActionResult DownloadBulkUploadTemplate()
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Employees");

        worksheet.Cell(1, 1).Value = "FullName";
        worksheet.Cell(1, 2).Value = "Email";
        worksheet.Cell(1, 3).Value = "Role";
        worksheet.Cell(1, 4).Value = "Designation";
        worksheet.Cell(1, 5).Value = "DepartmentName";
        worksheet.Cell(1, 6).Value = "DateOfBirth";

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

        worksheet.Cell(2, 1).Value = "Jane Doe";
        worksheet.Cell(2, 2).Value = "jane.doe@company.com";
        worksheet.Cell(2, 3).Value = "Employee";
        worksheet.Cell(2, 4).Value = "Software Engineer";
        worksheet.Cell(2, 5).Value = "Engineering";
        worksheet.Cell(2, 6).Value = "1995-08-15";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Employee_Bulk_Upload_Template.xlsx");
    }
}