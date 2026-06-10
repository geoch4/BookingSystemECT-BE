// Backend purpose: Defines the write-side application flow for CreateNotificationCommandHandler.
using System;
using System.Threading;
using System.Threading.Tasks;
using BookingSystem.Application.DTO;
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure;
using MediatR;

namespace BookingSystem.Application.Commands.NotificationCommand.CreateNotification;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, NotificationDto>
{
    private readonly AppDbContext _context;

    public CreateNotificationCommandHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationDto> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var entity = new Notification
        {
            Message = request.Dto.Message,
            RecipientId = request.Dto.RecipientId,
            SentAt = DateTime.UtcNow,
            IsRead = request.Dto.IsRead
        };

        _context.Notifications.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new NotificationDto
        {
            NotificationId = entity.NotificationId,
            Message = entity.Message,
            SentAt = entity.SentAt,
            RecipientId = entity.RecipientId,
            IsRead = entity.IsRead
        };
    }
}
