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

        RuleFor(x => x.DepartmentPriority)
            .InclusiveBetween(1, 3)
            .When(x => x.DepartmentPriority.HasValue)
            .WithMessage("DepartmentPriority must be between 1 and 3");

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

        RuleFor(x => x.ClinicalDetails!)
            .SetValidator(new ReferralClinicalDetailsDtoValidator())
            .When(x => x.ClinicalDetails is not null);

        When(x => x.Status == BookingSystem.Domain.Enums.BookingStatus.Cancelled, () =>
        {
            RuleFor(x => x.CancellationReason)
                .NotEmpty().WithMessage("CancellationReason is required when a booking is cancelled");
        });
    }
}

public class ReferralClinicalDetailsDtoValidator : AbstractValidator<ReferralClinicalDetailsDto>
{
    private static readonly string[] SessionPlans = ["single", "double"];

    public ReferralClinicalDetailsDtoValidator()
    {
        RuleFor(x => x.Indication).MaximumLength(2000);
        RuleFor(x => x.AnesthesiaInfo).MaximumLength(2000);
        RuleFor(x => x.SomaticRisk).MaximumLength(2000);
        RuleFor(x => x.BloodPressure).MaximumLength(40);
        RuleFor(x => x.WeightKg).MaximumLength(20);
        RuleFor(x => x.Allergies).MaximumLength(500);
        RuleFor(x => x.Medications).MaximumLength(2000);
        RuleFor(x => x.AdditionalInfo).MaximumLength(2000);
        RuleFor(x => x.PreviousTreatments).MaximumLength(2000);
        RuleFor(x => x.TreatmentGoal).MaximumLength(1000);

        RuleFor(x => x.RtmsSessionPlan)
            .Must(plan => plan is null || Array.Exists(SessionPlans, p => p == plan))
            .WithMessage("RtmsSessionPlan must be 'single' or 'double'");

        RuleFor(x => x.CompletedDocuments)
            .Must(docs => docs.Count <= 12 && docs.TrueForAll(d => d.Length <= 40 && !d.Contains(',')))
            .WithMessage("CompletedDocuments has too many or invalid entries");
    }
}
