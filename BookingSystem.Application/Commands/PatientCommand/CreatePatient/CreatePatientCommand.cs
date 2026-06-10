// Backend purpose: Defines the write-side application flow for CreatePatientCommand.
using BookingSystem.Application.DTO;
using MediatR;

namespace BookingSystem.Application.Commands.PatientCommand.CreatePatient;

public record CreatePatientCommand(CreatePatientDto Dto) : IRequest<PatientDto>;