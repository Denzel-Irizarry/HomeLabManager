using HomeLabManager.API.Interfaces;
using HomeLabManager.API.Interfaces.RemoteObservation;
using HomeLabManager.API.Models.RemoteObservation;
using HomeLabManager.API.Services.RemoteObservation;
using HomeLabManager.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HomeLabManager.API.Tests.Services.RemoteObservation
{
    public class RemoteObservationServiceTests
    {
        [Fact]
        public async Task ProbeAsync_WithInlineProfile_UsesProvider()
        {
            var profile = new DeviceConnectionProfile
            {
                Host = "192.0.2.1",
                Port = 22,
                Transport = "ssh",
                CredentialReference = "vault:creds/ssh-1",
                IdentityName = "admin"
            };

            var stubProvider = new StubProvider();
            var providers = new List<IRemoteObservationProvider> { stubProvider };

            var stubRepo = new StubDeviceRepo();
            var service = new RemoteObservationService(providers, stubRepo);

            var request = new RemoteObservationRequest { InlineProfile = profile };

            var result = await service.ProbeAsync(Guid.NewGuid(), request);

            Assert.True(result.Success);
            Assert.Equal("ok", result.Summary);
            Assert.True(stubProvider.WasCalled);
        }

        [Fact]
        public async Task ProbeAsync_NoProviderForTransport_ReturnsError()
        {
            var profile = new DeviceConnectionProfile
            {
                Host = "192.0.2.2",
                Transport = "unsupported",
                CredentialReference = "ref"
            };

            var providers = new List<IRemoteObservationProvider>();
            var stubRepo = new StubDeviceRepo();
            var service = new RemoteObservationService(providers, stubRepo);

            var request = new RemoteObservationRequest { InlineProfile = profile };
            var result = await service.ProbeAsync(Guid.NewGuid(), request);

            Assert.False(result.Success);
            Assert.Contains("No provider available", result.ErrorMessage ?? string.Empty);
        }

        private sealed class StubProvider : IRemoteObservationProvider
        {
            public bool WasCalled { get; private set; }

            public bool CanHandle(string transport) => string.Equals(transport, "ssh", StringComparison.OrdinalIgnoreCase);

            public Task<RemoteObservationResult> ObserveAsync(DeviceConnectionProfile profile, System.Threading.CancellationToken cancellationToken = default)
            {
                WasCalled = true;
                var result = new RemoteObservationResult
                {
                    Success = true,
                    Summary = "ok",
                    ConnectionProfile = profile,
                    Facts = new Dictionary<string, string> { { "os", "linux" } }
                };
                return Task.FromResult(result);
            }
        }

        private sealed class StubDeviceRepo : DeviceRepositoryInterface
        {
            public Task AddAsync(Core.Entities.Device device) => Task.CompletedTask;
            public Task<List<Core.Entities.Device>> GetAllAsync() => Task.FromResult(new List<Core.Entities.Device>());
            public Task<Core.Entities.Device?> GetDeviceByIdAsync(Guid id) => Task.FromResult<Core.Entities.Device?>(null);
            public Task<Core.Entities.Device?> GetForUpdateByIdAsync(Guid id) => Task.FromResult<Core.Entities.Device?>(null);
            public Task<bool> SerialExistsAsynch(string serial) => Task.FromResult(false);
            public Task<bool> DeleteByIdAsync(Guid id) => Task.FromResult(false);
        }
    }
}
