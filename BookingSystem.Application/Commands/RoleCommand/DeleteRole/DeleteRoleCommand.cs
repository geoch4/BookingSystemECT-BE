// Backend purpose: Defines the write-side application flow for DeleteRoleCommand.
using MediatR;

namespace BookingSystem.Application.Commands.RoleCommand.DeleteRole;

public record DeleteRoleCommand(int Id) : IRequest<bool>;