// Backend purpose: Defines the write-side application flow for DeleteUserCommand.
using MediatR;

namespace BookingSystem.Application.Commands.UserCommands.DeleteUser;


public record DeleteUserCommand(int UserId) : IRequest<bool>;
