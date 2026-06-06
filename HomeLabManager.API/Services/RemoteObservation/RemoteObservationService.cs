using HomeLabManager.API.Interfaces.RemoteObservation;
using HomeLabManager.API.Models.RemoteObservation;
using HomeLabManager.Core.Entities;
using HomeLabManager.API.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace HomeLabManager.API.Services.RemoteObservation
{
    public class RemoteObservationService : IRemoteObservationService
    {
        private readonly IEnumerable<IRemoteObservationProvider> _providers;
        private readonly DeviceRepositoryInterface _deviceRepository;

        public RemoteObservationService(IEnumerable<IRemoteObservationProvider> providers, DeviceRepositoryInterface deviceRepository)
        {
            _providers = providers;
            _deviceRepository = deviceRepository;
        }

        public async Task<RemoteObservationResponse> ProbeAsync(Guid deviceId, RemoteObservationRequest request, CancellationToken cancellationToken = default)
        {
            // Determine profile: prefer inline profile in the request.
            DeviceConnectionProfile? profile = request.InlineProfile;

            if (profile == null)
            {
                // If caller passed a deviceId, attempt to find stored profile on the device (not implemented yet).
                var device = await _deviceRepository.GetDeviceByIdAsync(deviceId);
                if (device == null)
                {
                    return new RemoteObservationResponse
                    {
                        Id = Guid.Empty,
                        ObservedAtUtc = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = "Device not found and no inline profile supplied."
                    };
                }

                // No stored connection profile yet: return an informative failure for the first slice.
                return new RemoteObservationResponse
                {
                    Id = Guid.Empty,
                    ObservedAtUtc = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = "No connection profile available for this device. Provide an inline profile or add a connection profile to the device."
                };
            }

            // Choose a provider by transport
            var provider = _providers.FirstOrDefault(p => p.CanHandle(profile.Transport));
            if (provider == null)
            {
                return new RemoteObservationResponse
                {
                    Id = Guid.Empty,
                    ObservedAtUtc = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = $"No provider available for transport '{profile.Transport}'."
                };
            }

            var result = await provider.ObserveAsync(profile, cancellationToken);

            return new RemoteObservationResponse
            {
                Id = result.Id,
                ObservedAtUtc = result.ObservedAtUtc,
                ConnectionProfile = result.ConnectionProfile,
                Facts = result.Facts,
                Summary = result.Summary,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage
            };
        }
    }
}
