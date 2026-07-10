using Application.DTO;
using Application.Interface;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/notifications")]
public class AdminNotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public AdminNotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("broadcast")]
    public async Task<ActionResult> BroadcastToCustomers([FromBody] SendNotificationRequest request)
    {
        await _notificationService.BroadcastToAllCustomersAsync(request.Title, request.Message, request.Link);
        return Ok();
    }

    [HttpPost("send")]
    public async Task<ActionResult> SendToUser([FromBody] SendNotificationRequest request)
    {
        if (request.UserId == null) return BadRequest("UserId is required.");
        
        await _notificationService.SendNotificationAsync(request.UserId.Value, request.Title, request.Message, NotificationType.Promotion, request.Link);
        return Ok();
    }
}
