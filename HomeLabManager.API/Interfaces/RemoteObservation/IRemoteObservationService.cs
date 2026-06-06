using HomeLabManager.API.Models.RemoteObservation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HomeLabManager.API.Interfaces.RemoteObservation
{
    // Service-facing contract for initiating a read-only observation.
    public interface IRemoteObservationService
    {
        Task<RemoteObservationResponse> ProbeAsync(Guid deviceId, RemoteObservationRequest request, CancellationToken cancellationToken = default);
    }
}
