namespace LeaveManagement.Application.DTOs.LeaveRequest;

public class CreateLeaveRequestDto
{
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty; 
    public string? RequestComments { get; set; }
}