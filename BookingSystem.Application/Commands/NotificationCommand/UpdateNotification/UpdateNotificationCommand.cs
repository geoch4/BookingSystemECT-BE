// Backend purpose: Defines the write-side application flow for UpdateNotificationCommand.
using BookingSystem.Application.DTO;
using MediatR;

namespace BookingSystem.Application.Commands.NotificationCommand.UpdateNotification;

public record UpdateNotificationCommand(int Id, CreateNotificationDto Dto) : IRequest<bool>;