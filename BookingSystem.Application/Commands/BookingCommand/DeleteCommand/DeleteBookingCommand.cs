// Backend purpose: Defines the write-side application flow for DeleteBookingCommand.
using MediatR;

namespace BookingSystem.Application.Commands.BookingCommand.DeleteCommand;

public record DeleteBookingCommand(int Id) : IRequest<bool>;