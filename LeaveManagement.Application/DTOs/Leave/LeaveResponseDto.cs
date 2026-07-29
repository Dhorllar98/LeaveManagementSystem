using LeaveManagement.Domain.Enums;

namespace LeaveManagement.Application.DTOs.Leave;

public class LeaveResponseDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DurationInDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; }
    public string? ManagerComments { get; set; }
    public DateTime CreatedAt { get; set; }
}