// Backend purpose: Defines the write-side application flow for UpdateRoleCommand.
using BookingSystem.Application.DTO;
using MediatR;

namespace BookingSystem.Application.Commands.RoleCommand.UpdateRole;

public record UpdateRoleCommand(int Id, CreateRoleDto Dto) : IRequest<bool>;