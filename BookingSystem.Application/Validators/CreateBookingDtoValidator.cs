// Backend purpose: Validates incoming data for CreateBookingDtoValidator before application logic runs.
using System;
using FluentValidation;
using BookingSystem.Application.DTO;

namespace BookingSystem.Application.Validators;

public class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
{
    public CreateBookingDtoValidator()
    {
        RuleFor(x => x.Date)
            .GreaterThan(DateTime.MinValue).WithMessage("Date is required");

        RuleFor(x => x.PatientId)
            .GreaterThan(0).WithMessage("PatientId must be valid");

        RuleFor(x => x.TreatmentType)
            .IsInEnum().WithMessage("TreatmentType must be a valid value");

        RuleFor(x => x.CreatedById)
            .GreaterThan(0).WithMessage("CreatedById must be valid");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status");

        RuleFor(x => x.HealthDeclarationStatus)
            .IsInEnum().WithMessage("Invalid health declaration status");

        RuleFor(x => x.AnesthesiaAssessmentStatus)
            .IsInEnum().WithMessage("Invalid anesthesia assessment status");

        RuleFor(x => x.ReferralDepartment)
            .MaximumLength(120).WithMessage("ReferralDepartment can be at most 120 characters");

        RuleFor(x => x.ReferralContactName)
            .MaximumLength(120).WithMessage("ReferralContactName can be at most 120 characters");

        RuleFor(x => x.ReferralContactPhone)
            .MaximumLength(40).WithMessage("ReferralContactPhone can be at most 40 characters");

        RuleFor(x => x.ReferralDepartment)
            .NotEmpty().WithMessage("ReferralDepartment is required");

        RuleFor(x => x.CancellationReason)
            .MaximumLength(500).WithMessage("CancellationReason can be at most 500 characters");

        RuleFor(x => x.ExternalPatientId)
            .MaximumLength(160).WithMessage("ExternalPatientId can be at most 160 characters");

        RuleFor(x => x.SourceSystem)
            .MaximumLength(80).WithMessage("SourceSystem can be at most 80 characters");

        RuleFor(x => x.CareUnitId)
            .MaximumLength(120).WithMessage("CareUnitId can be at most 120 characters");

        RuleFor(x => x.RegionCode)
            .MaximumLength(20).WithMessage("RegionCode can be at most 20 characters");

        RuleFor(x => x.IntegrationStatus)
            .MaximumLength(40).WithMessage("IntegrationStatus can be at most 40 characters");

        When(x => x.Status == BookingSystem.Domain.Enums.BookingStatus.Cancelled, () =>
        {
            RuleFor(x => x.CancellationReason)
                .NotEmpty().WithMessage("CancellationReason is required when a booking is cancelled");
        });
    }
}
