// Backend purpose: Provides domain helper methods for TreatmentTypeExtensions.
using BookingSystem.Domain.Enums;

namespace BookingSystem.Domain.Extensions;

public static class TreatmentTypeExtensions
{
    public static string ToSwedishString(this TreatmentType treatmentType) => treatmentType switch
    {
        TreatmentType.EctAkut       => "ECT - akut",
        TreatmentType.EctPlanerad   => "ECT - planerad",
        TreatmentType.EctUnderhall  => "ECT - underhåll",
        TreatmentType.EctIndexserie => "ECT - indexserie",
        TreatmentType.RtmsDepression => "rTMS - depression",
        TreatmentType.RtmsAngestOcd  => "rTMS - ångest/OCD",
        TreatmentType.RtmsUnderhall  => "rTMS - underhåll",
        _ => throw new ArgumentOutOfRangeException(nameof(treatmentType), treatmentType, null)
    };
}
