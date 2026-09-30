namespace BookingSystem.Application.Integrations;

public class IntegrationReadinessOptions
{
    public bool Enabled { get; set; }
    public string RegionCode { get; set; } = "VGR";
    public string SourceSystem { get; set; } = "Manual";
    public string IdentityProvider { get; set; } = "NotConfigured";
    public string JournalProvider { get; set; } = "NotConfigured";
    public string IntegrationLayer { get; set; } = "NotConfigured";
    public bool AllowOutboundPatientData { get; set; }
    public bool AllowPatientDetailsInExternalNotifications { get; set; }
}
