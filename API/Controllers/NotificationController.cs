// Backend purpose: Exposes HTTP endpoints for the NotificationController backend feature.
using BookingSystem.Application.Commands.NotificationCommand.CreateNotification;
using BookingSystem.Application.Commands.NotificationCommand.DeleteNotification;
using BookingSystem.Application.Commands.NotificationCommand.UpdateNotification;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Queries.QueriesNotification.GetAllNotifications;
using BookingSystem.Application.Queries.QueriesNotification.GetNotificationById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// TODO: Return OperationResult consistently from this controller flow.
// TODO: Align the corresponding MediatR handlers with OperationResult.

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
    {
        var notification = await _mediator.Send(new CreateNotificationCommand(dto));
        return CreatedAtAction(nameof(GetNotificationById), new { id = notification.NotificationId }, notification);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotificationById(int id)
    {
        var notification = await _mediator.Send(new GetNotificationByIdQuery(id));
        if (notification == null) return NotFound();
        return Ok(notification);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllNotifications([FromQuery] int? recipientId)
    {
        var notifications = await _mediator.Send(new GetAllNotificationsQuery());
        // Callers can request only the current user's notifications instead of receiving every recipient's data.
        if (recipientId.HasValue)
            notifications = notifications.Where(n => n.RecipientId == recipientId.Value);

        return Ok(notifications);
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var notification = await _mediator.Send(new GetNotificationByIdQuery(id), cancellationToken);
        if (notification == null) return NotFound();

        // Reuse the existing update command so read-state changes follow the same persistence path.
        notification.IsRead = true;
        var updated = await _mediator.Send(
            new UpdateNotificationCommand(id, new CreateNotificationDto
            {
                Message = notification.Message,
                RecipientId = notification.RecipientId,
                IsRead = true
            }),
            cancellationToken);

        return updated ? NoContent() : NotFound();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNotification(int id, [FromBody] CreateNotificationDto dto)
    {
        var updated = await _mediator.Send(new UpdateNotificationCommand(id, dto));
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var deleted = await _mediator.Send(new DeleteNotificationCommand(id));
        if (!deleted) return NotFound();
        return Ok(new { message = "Notification successfully deleted" });
    }
}
