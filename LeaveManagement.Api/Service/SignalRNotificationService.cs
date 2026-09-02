using LeaveManagement.Api.Hubs;
using LeaveManagement.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LeaveManagement.Api.Services;

public class SignalRNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationToUserAsync(Guid userId, string title, string message)
    {
        await _hubContext.Clients.User(userId.ToString())
            .SendAsync("ReceiveNotification", new { title, message, createdAt = DateTime.UtcNow });
    }

    public async Task SendNotificationToGroupAsync(string groupName, string title, string message)
    {
        await _hubContext.Clients.Group(groupName)
            .SendAsync("ReceiveNotification", new { title, message, createdAt = DateTime.UtcNow });
    }
}