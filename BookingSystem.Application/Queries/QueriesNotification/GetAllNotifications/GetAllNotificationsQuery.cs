// Backend purpose: Defines the read-side application flow for GetAllNotificationsQuery.
using System.Collections.Generic;
using BookingSystem.Application.DTO;
using MediatR;

namespace BookingSystem.Application.Queries.QueriesNotification.GetAllNotifications;

public record GetAllNotificationsQuery() : IRequest<IEnumerable<NotificationDto>>;