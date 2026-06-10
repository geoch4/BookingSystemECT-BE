// Backend purpose: Defines the read-side application flow for GetAllUsersQuery.
using System.Collections.Generic;
using BookingSystem.Application.DTO;
using MediatR;

namespace BookingSystem.Application.Queries.GetAllUsers;


    public record GetAllUsersQuery : IRequest<IEnumerable<UserDto>>;
