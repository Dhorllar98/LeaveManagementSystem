namespace LeaveManagement.Application.Interfaces;

public interface IRealtimeNotificationService
{
    Task SendNotificationToUserAsync(Guid userId, string title, string message);
    Task SendNotificationToGroupAsync(string groupName, string title, string message);
}