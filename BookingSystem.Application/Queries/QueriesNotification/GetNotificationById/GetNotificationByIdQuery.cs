// Backend purpose: Defines the read-side application flow for GetNotificationByIdQuery.
using BookingSystem.Application.DTO;
using MediatR;

namespace BookingSystem.Application.Queries.QueriesNotification.GetNotificationById;

public record GetNotificationByIdQuery(int Id) : IRequest<NotificationDto?>;