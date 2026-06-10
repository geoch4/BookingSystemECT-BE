// Backend purpose: Defines the write-side application flow for DeleteNotificationCommand.
using MediatR;

namespace BookingSystem.Application.Commands.NotificationCommand.DeleteNotification;

public record DeleteNotificationCommand(int Id) : IRequest<bool>;