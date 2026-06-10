// Backend purpose: Defines the read-side application flow for GetUserByIdQuery.
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using MediatR;

namespace BookingSystem.Application.Queries.User.GetUsersById;


public record GetUsersByIdQuery(int Id) : IRequest<OperationResult<UserDto>>;
