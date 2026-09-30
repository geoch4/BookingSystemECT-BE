// Backend purpose: Validates incoming data for CreatePatientDtoValidator before application logic runs.
using FluentValidation;
using BookingSystem.Application.DTO;

namespace BookingSystem.Application.Validators;

public class CreatePatientDtoValidator : AbstractValidator<CreatePatientDto>
{
    public CreatePatientDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required");

        RuleFor(x => x.SocialSecurityNumber)
            .NotEmpty().WithMessage("Social security number is required")
            .Matches(@"^\d{6}-\d{4}$")
            .WithMessage("SSN must be in format YYMMDD-XXXX");

        RuleFor(x => x.ExternalPatientId)
            .MaximumLength(160).WithMessage("ExternalPatientId can be at most 160 characters");

        RuleFor(x => x.SourceSystem)
            .MaximumLength(80).WithMessage("SourceSystem can be at most 80 characters");

        RuleFor(x => x.RegionCode)
            .MaximumLength(20).WithMessage("RegionCode can be at most 20 characters");

        RuleFor(x => x.IntegrationStatus)
            .MaximumLength(40).WithMessage("IntegrationStatus can be at most 40 characters");
    }
}
