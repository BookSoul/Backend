using Application.DTO;
using Application.Interface;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Hubs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace WebAPI.Services;

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(IAppDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(Guid userId, string title, string message, NotificationType type, string? link = null)
    {
        var notif = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Link = link
        };
        _context.Notifications.Add(notif);
        await _context.SaveChangesAsync();

        var dto = new NotificationDto(notif.Id, notif.UserId, notif.Title, notif.Message, notif.Link, notif.Type, notif.IsRead, notif.CreatedAt);
        await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", dto);
    }

    public async Task BroadcastToRoleAsync(string roleName, string title, string message, NotificationType type, string? link = null)
    {
        // For roles like Admin/Staff, we might not save to DB individually if we want it to be transient, 
        // OR we can save to DB for all users in that role.
        // For simplicity, we just send via SignalR for now (transient).
        var dto = new NotificationDto(Guid.NewGuid(), Guid.Empty, title, message, link, type, false, DateTime.UtcNow);
        await _hubContext.Clients.Group($"Role_{roleName}").SendAsync("ReceiveNotification", dto);
    }

    public async Task BroadcastToAllCustomersAsync(string title, string message, string? link = null)
    {
        // Transient broadcast to all Customers
        var dto = new NotificationDto(Guid.NewGuid(), Guid.Empty, title, message, link, NotificationType.Promotion, false, DateTime.UtcNow);
        await _hubContext.Clients.Group("Role_Customer").SendAsync("ReceiveNotification", dto);
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId)
    {
        var list = await _context.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
            
        return list.Select(x => new NotificationDto(x.Id, x.UserId, x.Title, x.Message, x.Link, x.Type, x.IsRead, x.CreatedAt)).ToList();
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notif = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);
        if (notif != null)
        {
            notif.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var notifs = await _context.Notifications.Where(x => x.UserId == userId && !x.IsRead).ToListAsync();
        foreach (var n in notifs)
        {
            n.IsRead = true;
        }
        if (notifs.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}
