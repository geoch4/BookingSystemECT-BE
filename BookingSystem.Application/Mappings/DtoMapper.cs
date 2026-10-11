using System;
using BookingSystem.Application.DTO;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Application.Mappings;

public static class DtoMapper
{
    public static BookingDto ToDto(Booking booking)
    {
        return new BookingDto
        {
            BookingId = booking.BookingId,
            Date = booking.Date,
            PatientId = booking.PatientId,
            TreatmentType = booking.TreatmentType,
            CreatedById = booking.CreatedByUserId,
            CreatedAt = booking.CreatedAt,
            Priority = booking.Priority,
            Status = booking.Status,
            DepartmentPriority = booking.DepartmentPriority,
            HealthDeclarationStatus = booking.HealthDeclarationStatus,
            AnesthesiaAssessmentStatus = booking.AnesthesiaAssessmentStatus,
            ReferralDepartment = booking.ReferralDepartment,
            ReferralContactName = booking.ReferralContactName,
            ReferralContactPhone = booking.ReferralContactPhone,
            CancellationReason = booking.CancellationReason,
            ExternalPatientId = booking.ExternalPatientId,
            SourceSystem = booking.SourceSystem,
            CareUnitId = booking.CareUnitId,
            DepartmentId = booking.DepartmentId,
            TreatmentUnitId = booking.TreatmentUnitId,
            RegionCode = booking.RegionCode,
            IntegrationStatus = booking.IntegrationStatus.ToString()
        };
    }

    public static PatientDto ToDto(Patient patient)
    {
        return new PatientDto
        {
            PatientId = patient.PatientId,
            FullName = patient.FullName,
            SocialSecurityNumber = patient.SocialSecurityNumber,
            ExternalPatientId = patient.ExternalPatientId,
            SourceSystem = patient.SourceSystem,
            RegionCode = patient.RegionCode,
            IntegrationStatus = patient.IntegrationStatus.ToString()
        };
    }

    public static IntegrationStatus ParseIntegrationStatus(string? value)
    {
        return Enum.TryParse<IntegrationStatus>(value, ignoreCase: true, out var status)
            ? status
            : IntegrationStatus.Manual;
    }

    public static string CleanSourceSystem(string? sourceSystem)
    {
        return string.IsNullOrWhiteSpace(sourceSystem) ? "Manual" : sourceSystem.Trim();
    }
}
