// Backend purpose: Defines the read-side application flow for GetRoleByIdQuery.
using BookingSystem.Application.DTO;
using MediatR;

namespace BookingSystem.Application.Queries.QueriesRole.GetRolesById;

public record GetRoleByIdQuery(int Id) : IRequest<RoleDto?>;