// Backend purpose: Defines the write-side application flow for DeletePatientCommand.
using MediatR;

namespace BookingSystem.Application.Commands.PatientCommand.DeletePatient;

public record DeletePatientCommand(int Id) : IRequest<bool>;