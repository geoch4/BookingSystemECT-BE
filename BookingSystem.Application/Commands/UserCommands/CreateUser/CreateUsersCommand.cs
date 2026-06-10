// Backend purpose: Defines the write-side application flow for CreateUsersCommand.
using BookingSystem.Application.Common;
using BookingSystem.Application.DTO;
using MediatR;

namespace BookingSystem.Application.Commands.UserCommands.CreateUser;

public record CreateUserCommand(CreateUserDto Dto) : IRequest<OperationResult<int>>;
