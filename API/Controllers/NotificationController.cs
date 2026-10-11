// Backend purpose: Exposes HTTP endpoints for the NotificationController backend feature.
using BookingSystem.Application.Commands.NotificationCommand.CreateNotification;
using BookingSystem.Application.Commands.NotificationCommand.DeleteNotification;
using BookingSystem.Application.Commands.NotificationCommand.UpdateNotification;
using BookingSystem.Application.DTO;
using BookingSystem.Application.Authorization;
using BookingSystem.Application.Queries.QueriesNotification.GetAllNotifications;
using BookingSystem.Application.Queries.QueriesNotification.GetNotificationById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Controllers;

// TODO: Return OperationResult consistently from this controller flow.
// TODO: Align the corresponding MediatR handlers with OperationResult.

[ApiController]
[Authorize(Policy = AccessPolicies.CareStaff)]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = AccessPolicies.OnlyAdmin)]
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
        if (!CanAccessNotification(notification)) return Forbid();

        return Ok(notification);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllNotifications([FromQuery] int? recipientId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null) return Unauthorized();

        if (recipientId.HasValue && recipientId.Value != currentUserId.Value && !IsAdmin())
            return Forbid();

        var targetRecipientId = recipientId.HasValue && IsAdmin()
            ? recipientId.Value
            : currentUserId.Value;

        var notifications = await _mediator.Send(new GetAllNotificationsQuery());
        notifications = notifications
            .Where(n => n.RecipientId == targetRecipientId)
            .OrderByDescending(n => n.SentAt);

        return Ok(notifications);
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken cancellationToken)
    {
        var notification = await _mediator.Send(new GetNotificationByIdQuery(id), cancellationToken);
        if (notification == null) return NotFound();
        if (!CanAccessNotification(notification)) return Forbid();

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

    [Authorize(Policy = AccessPolicies.OnlyAdmin)]
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
        var notification = await _mediator.Send(new GetNotificationByIdQuery(id));
        if (notification == null) return NotFound();
        if (!CanAccessNotification(notification)) return Forbid();

        var deleted = await _mediator.Send(new DeleteNotificationCommand(id));
        if (!deleted) return NotFound();
        return Ok(new { message = "Notification successfully deleted" });
    }

    private bool CanAccessNotification(NotificationDto notification)
    {
        var currentUserId = GetCurrentUserId();
        return currentUserId is not null &&
               (notification.RecipientId == currentUserId.Value || IsAdmin());
    }

    private int? GetCurrentUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? User.FindFirstValue("sub");

        return int.TryParse(id, out var userId) ? userId : null;
    }

    private bool IsAdmin()
    {
        return AccessRoles.IsAdmin(User.FindFirstValue(ClaimTypes.Role));
    }
}
