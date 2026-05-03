using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ecom.Notification.Application.DTOs;
using Ecom.Notification.Application.Interfaces;

namespace Ecom.Notification.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/notification")]
[ApiVersion("1.0")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationController(INotificationService service)
    {
        _service = service;
    }

    [Authorize] // 🔒 SECURITY: Changed from AllowAnonymous - internal service-to-service only
    [HttpPost("send")]
    public async Task<IActionResult> Send(NotificationRequest request)
    {
        var result = await _service.SendNowAsync(request);
        return result ? Ok("Email Sent ✅") : BadRequest("Failed ❌");
    }

    [Authorize] // 🔒 SECURITY: Changed from AllowAnonymous - internal service-to-service only
    [HttpPost("schedule")]
    public async Task<IActionResult> Schedule(ScheduleNotificationRequest request)
    {
        if (!request.ScheduledAtIST.HasValue)
            return BadRequest("ScheduledAtIST required ❌");

        var result = await _service.ScheduleAsync(request);
        return result
            ? Ok("Email Scheduled ⏳")
            : BadRequest("Invalid schedule time. Use a future IST datetime.");
    }
}

