// Backend purpose: Defines the write-side application flow for DeleteDocumentCommand.
using BookingSystem.Application.Common;
using MediatR;

namespace BookingSystem.Application.Commands.DocumentCommand.DeleteDocument;

public record DeleteDocumentCommand(int Id) : IRequest<OperationResult<bool>>;