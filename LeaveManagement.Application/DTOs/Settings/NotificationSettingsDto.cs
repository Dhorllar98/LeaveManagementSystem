namespace LeaveManagement.Application.DTOs.Settings;

public class NotificationSettingsDto
{
    public bool EnableNewLeaveRequestEmails { get; set; } = true;
    public bool EnableLeaveStatusUpdateEmails { get; set; } = true;
}