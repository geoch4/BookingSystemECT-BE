// Backend purpose: Defines the write-side application flow for LogInUserCommand.
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using MediatR;

namespace BookingSystem.Application.Commands.UserCommands.LogInUser;

public record LogInUserCommand(LoginDto Dto) : IRequest<OperationResult<string>>;
