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
    }
}
