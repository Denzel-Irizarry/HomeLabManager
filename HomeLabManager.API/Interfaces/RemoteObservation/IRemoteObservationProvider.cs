using HomeLabManager.Core.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace HomeLabManager.API.Interfaces.RemoteObservation
{
    /// <summary>
    /// Provider contract for a transport implementation (SSH, agent, etc.).
    /// Implementations that support SSH may use the SSH.NET library (NuGet package <c>SSH.NET</c>)
    /// to perform network operations. Credentials for SSH providers should be resolved via
    /// <c>ICredentialResolver</c> and must not be persisted by providers.
    /// </summary>
    public interface IRemoteObservationProvider
    {
        /// <summary>
        /// Returns true if this provider can handle the given transport string (e.g., "ssh", "agent").
        /// </summary>
        bool CanHandle(string transport);

        /// <summary>
        /// Perform a read-only observation using the provided profile.
        /// For SSH transports, implementations should treat the operation as non-destructive and
        /// use safe read-only commands. Providers must respect the provided <see cref="CancellationToken"/>.
        /// </summary>
        Task<RemoteObservationResult> ObserveAsync(DeviceConnectionProfile profile, CancellationToken cancellationToken = default);
    }
}
