using System.Collections.Generic;

namespace BookingSystem.Application.DTO;

public class IntegrationReadinessDto
{
    public bool Enabled { get; set; }
    public string RegionCode { get; set; } = "VGR";
    public string SourceSystem { get; set; } = "Manual";
    public string IdentityProvider { get; set; } = "NotConfigured";
    public string JournalProvider { get; set; } = "NotConfigured";
    public string IntegrationLayer { get; set; } = "NotConfigured";
    public bool AllowOutboundPatientData { get; set; }
    public bool AllowPatientDetailsInExternalNotifications { get; set; }
    public List<IntegrationCapabilityDto> Capabilities { get; set; } = [];
}

public class IntegrationCapabilityDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Status { get; set; } = "NotConfigured";
    public string Detail { get; set; } = string.Empty;
}
