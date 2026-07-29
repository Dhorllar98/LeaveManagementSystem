namespace LeaveManagement.Application.DTOs.Leave;

public class UpdateLeaveDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}