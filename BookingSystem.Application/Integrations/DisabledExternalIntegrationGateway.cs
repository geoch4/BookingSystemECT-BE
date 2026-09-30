using System.Threading;
using System.Threading.Tasks;
using BookingSystem.Application.DTO;
using Microsoft.Extensions.Options;

namespace BookingSystem.Application.Integrations;

public class DisabledExternalIntegrationGateway : IExternalIntegrationGateway
{
    private readonly IntegrationReadinessOptions _options;

    public DisabledExternalIntegrationGateway(IOptions<IntegrationReadinessOptions> options)
    {
        _options = options.Value;
    }

    public Task<IntegrationReadinessDto> GetReadinessAsync(CancellationToken cancellationToken)
    {
        var readiness = new IntegrationReadinessDto
        {
            Enabled = _options.Enabled,
            RegionCode = Clean(_options.RegionCode, "VGR"),
            SourceSystem = Clean(_options.SourceSystem, "Manual"),
            IdentityProvider = Clean(_options.IdentityProvider, "NotConfigured"),
            JournalProvider = Clean(_options.JournalProvider, "NotConfigured"),
            IntegrationLayer = Clean(_options.IntegrationLayer, "NotConfigured"),
            AllowOutboundPatientData = _options.AllowOutboundPatientData,
            AllowPatientDetailsInExternalNotifications = _options.AllowPatientDetailsInExternalNotifications,
            Capabilities =
            [
                Capability("identity", "Identitet/SSO", _options.IdentityProvider),
                Capability("journal", "Journalsystem", _options.JournalProvider),
                Capability("integration-layer", "Regionalt integrationslager", _options.IntegrationLayer),
                new()
                {
                    Key = "outbound-patient-data",
                    Label = "Utgående patientdata",
                    Status = _options.AllowOutboundPatientData ? "AllowedByConfig" : "BlockedByDefault",
                    Detail = _options.AllowOutboundPatientData
                        ? "Konfiguration tillåter utgående patientdata, men ingen aktiv anslutning finns här."
                        : "Utgående patientdata är spärrad tills juridik, avtal och teknisk väg är godkända."
                },
                new()
                {
                    Key = "external-notification-details",
                    Label = "Patientdetaljer i externa notiser",
                    Status = _options.AllowPatientDetailsInExternalNotifications ? "AllowedByConfig" : "BlockedByDefault",
                    Detail = _options.AllowPatientDetailsInExternalNotifications
                        ? "Konfiguration tillåter patientdetaljer i externa notiser. Använd bara efter godkänd policy."
                        : "Externa notiser ska inte innehålla känsliga patientdetaljer."
                }
            ]
        };

        return Task.FromResult(readiness);
    }

    private static IntegrationCapabilityDto Capability(string key, string label, string? configuredValue)
    {
        var value = Clean(configuredValue, "NotConfigured");
        return new IntegrationCapabilityDto
        {
            Key = key,
            Label = label,
            Status = value == "NotConfigured" ? "NotConfigured" : "Configured",
            Detail = value == "NotConfigured"
                ? "Ingen aktiv integration är konfigurerad."
                : $"Förberett värde: {value}. Ingen patientdata skickas av denna gateway."
        };
    }

    private static string Clean(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
