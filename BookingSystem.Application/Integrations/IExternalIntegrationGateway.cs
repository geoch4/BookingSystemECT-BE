using System.Threading;
using System.Threading.Tasks;
using BookingSystem.Application.DTO;

namespace BookingSystem.Application.Integrations;

public interface IExternalIntegrationGateway
{
    Task<IntegrationReadinessDto> GetReadinessAsync(CancellationToken cancellationToken);
}
