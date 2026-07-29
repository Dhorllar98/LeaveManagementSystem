namespace LeaveManagement.Application.DTOs.LeaveType;

public class CreateLeaveTypeDto
{
    public string Name { get; set; } = string.Empty;
    public int DefaultDays { get; set; }
}