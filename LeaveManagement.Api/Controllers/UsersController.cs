using LeaveManagement.Application.DTOs.Auth;
using LeaveManagement.Application.Interfaces;
using LeaveManagement.Domain.Entities;
using LeaveManagement.Domain.Enums;
using LeaveManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveManagement.Api.Controllers;

[Authorize]
public class UsersController : BaseController
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Department,
                u.Designation,
                Role = u.Role.ToString(),
                u.LeaveBalance
            })
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost("provision")]
    [Authorize(Roles = "Admin,Manager")] 
    public async Task<IActionResult> ProvisionUser(
    [FromBody] AdminCreateUserDto dto,
    [FromServices] IEmailService emailService,
    CancellationToken cancellationToken)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, cancellationToken);
        if (existingUser != null)
        {
            return BadRequest(new { message = "User with this email already exists." });
        }

        string defaultPassword = "Welcome" + Random.Shared.Next(1000, 9999) + "!";
        string resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                            .Replace("+", "")
                            .Replace("/", "")
                            .Replace("=", "");

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
            Department = dto.Department,
            Designation = dto.Designation,
            Role = Enum.TryParse<UserRole>(dto.Role, true, out var role) ? role : UserRole.Employee,
            LeaveBalance = 20,
            PasswordResetToken = resetToken,
            ResetTokenExpiresAt = DateTime.UtcNow.AddHours(48),
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(newUser, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        string baseUrl = !string.IsNullOrWhiteSpace(dto.ClientResetUrl)  
            ? dto.ClientResetUrl
            : "https://your-frontend-app.com/reset-password";

        string resetLink = $"{baseUrl}?email={Uri.EscapeDataString(dto.Email)}&token={resetToken}";

        string emailBody = $@"
            <h3>Welcome to the Leave Management System, {dto.FullName}!</h3>
            <p>An account has been created for you by your Administrator.</p>
            <p><strong>Temporary Password:</strong> <code>{defaultPassword}</code></p>
            <p>Please click the link below to set your new password:</p>
            <p><a href='{resetLink}' style='background-color: #4CAF50; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px; display: inline-block;'>Set New Password</a></p>";

        await emailService.SendEmailAsync(dto.Email, "Account Created - Set Your Password", emailBody);

        return Ok(new
        {
            success = true,
            message = "User created successfully and invitation email sent.",
            data = new
            {
                userId = newUser.Id,
                email = newUser.Email,
                defaultPassword,
                resetToken
            }
        });
    }
}