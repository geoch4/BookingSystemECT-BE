// Backend purpose: Defines the write-side application flow for CreateNotificationCommand.
using BookingSystem.Application.DTO;
using MediatR;

namespace BookingSystem.Application.Commands.NotificationCommand.CreateNotification;

public record CreateNotificationCommand(CreateNotificationDto Dto) : IRequest<NotificationDto>;