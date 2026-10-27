using System;
using System.Collections.Generic;
using System.Linq;
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
            RequestedStartDate = booking.RequestedStartDate,
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

    public static ReferralClinicalDetails ToEntity(ReferralClinicalDetailsDto dto)
    {
        return new ReferralClinicalDetails
        {
            Indication = Clean(dto.Indication),
            AnesthesiaInfo = Clean(dto.AnesthesiaInfo),
            SomaticRisk = Clean(dto.SomaticRisk),
            FastingConfirmed = dto.FastingConfirmed,
            BloodPressure = Clean(dto.BloodPressure),
            WeightKg = Clean(dto.WeightKg),
            Allergies = Clean(dto.Allergies),
            Medications = Clean(dto.Medications),
            AdditionalInfo = Clean(dto.AdditionalInfo),
            RtmsSessionPlan = Clean(dto.RtmsSessionPlan),
            PreviousTreatments = Clean(dto.PreviousTreatments),
            TreatmentGoal = Clean(dto.TreatmentGoal),
            ContraindicationChecklistComplete = dto.ContraindicationChecklistComplete,
            MetalImplantsOrDevices = dto.MetalImplantsOrDevices,
            SeizureHistory = dto.SeizureHistory,
            CompletedDocuments = dto.CompletedDocuments.Count == 0
                ? null
                : string.Join(",", dto.CompletedDocuments.Select(d => d.Trim()).Where(d => d.Length > 0).Distinct())
        };
    }

    public static ReferralClinicalDetailsDto ToDto(ReferralClinicalDetails details)
    {
        return new ReferralClinicalDetailsDto
        {
            Indication = details.Indication,
            AnesthesiaInfo = details.AnesthesiaInfo,
            SomaticRisk = details.SomaticRisk,
            FastingConfirmed = details.FastingConfirmed,
            BloodPressure = details.BloodPressure,
            WeightKg = details.WeightKg,
            Allergies = details.Allergies,
            Medications = details.Medications,
            AdditionalInfo = details.AdditionalInfo,
            RtmsSessionPlan = details.RtmsSessionPlan,
            PreviousTreatments = details.PreviousTreatments,
            TreatmentGoal = details.TreatmentGoal,
            ContraindicationChecklistComplete = details.ContraindicationChecklistComplete,
            MetalImplantsOrDevices = details.MetalImplantsOrDevices,
            SeizureHistory = details.SeizureHistory,
            CompletedDocuments = string.IsNullOrWhiteSpace(details.CompletedDocuments)
                ? new List<string>()
                : details.CompletedDocuments.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        };
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
