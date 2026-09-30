namespace BookingSystem.Domain.Enums;

public enum IntegrationStatus
{
    Manual = 0,
    PendingSync = 1,
    Synced = 2,
    SyncFailed = 3,
    ExternalOnly = 4
}
