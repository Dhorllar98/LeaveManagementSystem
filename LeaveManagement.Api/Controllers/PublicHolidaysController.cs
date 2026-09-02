using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PublicHolidaysController : BaseController
{
    private readonly IAppDbContext _context;
    private readonly IUserRepository _userRepository;

    public PublicHolidaysController(IAppDbContext context, IUserRepository userRepository)
    {
        _context = context;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetHolidays([FromQuery] int? year, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null || !user.OrganizationId.HasValue) return Unauthorized();

        int targetYear = year ?? DateTime.UtcNow.Year;

        var holidays = await _context.PublicHolidays
            .AsNoTracking()
            .Where(ph => ph.OrganizationId == user.OrganizationId.Value && ph.Date.Year == targetYear)
            .OrderBy(ph => ph.Date)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            message = "Public holidays retrieved successfully.",
            data = holidays
        });
    }

    [HttpPost]
    [Authorize(Roles = "HR, Admin")]
    public async Task<IActionResult> CreateHoliday([FromBody] CreatePublicHolidayDto dto, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null || !user.OrganizationId.HasValue) return Unauthorized();

        var holiday = new PublicHoliday
        {
            OrganizationId = user.OrganizationId.Value,
            Name = dto.Name,
            Date = dto.Date.Date
        };

        _context.PublicHolidays.Add(holiday);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetHolidays), new { id = holiday.Id }, new
        {
            success = true,
            message = "Public holiday created successfully.",
            data = holiday
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "HR, Admin")]
    public async Task<IActionResult> DeleteHoliday(Guid id, CancellationToken cancellationToken)
    {
        var holiday = await _context.PublicHolidays.FindAsync(new object[] { id }, cancellationToken);
        if (holiday == null) return NotFound(new { message = "Public holiday not found." });

        _context.PublicHolidays.Remove(holiday);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, message = "Public holiday deleted successfully." });
    }
}

public class CreatePublicHolidayDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}