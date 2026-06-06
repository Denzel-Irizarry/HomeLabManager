using HomeLabManager.API.Interfaces.RemoteObservation;
using HomeLabManager.Core.Entities;
using HomeLabManager.API.Interfaces.Security;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace HomeLabManager.API.Services.RemoteObservation.Providers
{
    /// <summary>
    /// Real SSH provider implementation using SSH.NET (NuGet package <c>SSH.NET</c>).
    /// The provider resolves credentials via <c>ICredentialResolver</c> (expected format: "username:password")
    /// and performs read-only probes against the remote host using safe commands.
    /// </summary>
    public class SshRemoteObservationProvider : IRemoteObservationProvider
    {
        private readonly ICredentialResolver _credentialResolver;

        public SshRemoteObservationProvider(ICredentialResolver credentialResolver)
        {
            _credentialResolver = credentialResolver;
        }

        /// <inheritdoc/>
        public bool CanHandle(string transport) => string.Equals(transport, "ssh", StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc/>
        /// <remarks>
        /// This implementation uses password-based authentication derived from the resolved credential string.
        /// Providers should not store credentials and must return clear error messages for auth failures.
        /// </remarks>
        public async Task<RemoteObservationResult> ObserveAsync(DeviceConnectionProfile profile, CancellationToken cancellationToken = default)
        {
            var result = new RemoteObservationResult
            {
                ObservedAtUtc = DateTime.UtcNow,
                ConnectionProfile = profile,
                Success = false
            };

            if (string.IsNullOrWhiteSpace(profile.Host))
            {
                result.ErrorMessage = "Profile missing host.";
                return result;
            }

            string? resolved = null;
            if (!string.IsNullOrWhiteSpace(profile.CredentialReference))
            {
                try
                {
                    resolved = await _credentialResolver.ResolveCredentialAsync(profile.CredentialReference);
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = "Failed to resolve credentials: " + ex.Message;
                    return result;
                }
            }

            if (string.IsNullOrWhiteSpace(resolved))
            {
                result.ErrorMessage = "No credentials available for SSH connection.";
                return result;
            }

            // Expect resolved format "username:password" for Phase 2. Do not persist this.
            // Credentials are supplied by the configured `ICredentialResolver` and passed
            // directly to the SSH.NET client for authentication (password auth used here).
            var parts = resolved.Split(':', 2);
            if (parts.Length < 2)
            {
                result.ErrorMessage = "Unsupported credential format from resolver.";
                return result;
            }

            var username = parts[0];
            var secret = parts[1];
            var port = profile.Port ?? 22;

            try
            {
                // Determine authentication method: password or private key (private key content includes the PEM header)
                AuthenticationMethod authMethod;
                if (secret.Contains("-----BEGIN") )
                {
                    // Treat secret as a PEM private key content
                    var keyBytes = System.Text.Encoding.UTF8.GetBytes(secret);
                    using var ms = new System.IO.MemoryStream(keyBytes);
                    var pkFile = new PrivateKeyFile(ms);
                    authMethod = new PrivateKeyAuthenticationMethod(username, pkFile);
                }
                else
                {
                    // Default to password auth
                    authMethod = new PasswordAuthenticationMethod(username, secret);
                }

                var connInfo = new Renci.SshNet.ConnectionInfo(profile.Host, port, username, authMethod)
                {
                    Timeout = TimeSpan.FromSeconds(10)
                };

                using var client = new SshClient(connInfo);
                client.Connect();

                if (TryCollectUnixFacts(client, out var unixFacts))
                {
                    client.Disconnect();

                    result.Facts = unixFacts;
                    result.Success = true;
                    result.Summary = "SSH probe succeeded (Unix-like target)";
                    return result;
                }

                if (TryCollectWindowsFacts(client, out var windowsFacts))
                {
                    client.Disconnect();

                    result.Facts = windowsFacts;
                    result.Success = true;
                    result.Summary = "SSH probe succeeded (Windows target)";
                    return result;
                }

                client.Disconnect();

                result.ErrorMessage = "SSH probe failed: target OS was not recognized or commands were unavailable.";
                return result;
            }
            catch (SshAuthenticationException ex)
            {
                result.ErrorMessage = "SSH auth failed: " + ex.Message;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "SSH probe failed: " + ex.Message;
                return result;
            }
        }

        private static bool TryCollectUnixFacts(SshClient client, out Dictionary<string, string> facts)
        {
            facts = new Dictionary<string, string>();

            if (!TryRunCommand(client, "uname -s", out var osName) || string.IsNullOrWhiteSpace(osName))
                return false;

            facts["os_family"] = "unix-like";
            facts["os_name"] = osName.Trim();

            AddFactIfAvailable(facts, "hostname", client, "hostname");
            AddFactIfAvailable(facts, "kernel_release", client, "uname -r");
            AddFactIfAvailable(facts, "uptime", client, "uptime -p");

            return true;
        }

        private static bool TryCollectWindowsFacts(SshClient client, out Dictionary<string, string> facts)
        {
            facts = new Dictionary<string, string>();

            if (!TryRunCommand(client, "cmd /c ver", out var versionBanner) || string.IsNullOrWhiteSpace(versionBanner))
                return false;

            facts["os_family"] = "windows";
            facts["os_name"] = versionBanner.Trim();

            AddFactIfAvailable(facts, "hostname", client, "hostname");
            AddFactIfAvailable(facts, "edition", client, "powershell -NoProfile -Command \"(Get-CimInstance Win32_OperatingSystem).Caption\"");
            AddFactIfAvailable(facts, "uptime", client, "powershell -NoProfile -Command \"((Get-Date) - (Get-CimInstance Win32_OperatingSystem).LastBootUpTime).ToString()\"");

            return true;
        }

        private static void AddFactIfAvailable(Dictionary<string, string> facts, string key, SshClient client, string commandText)
        {
            if (TryRunCommand(client, commandText, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                facts[key] = value.Trim();
            }
        }

        private static bool TryRunCommand(SshClient client, string commandText, out string output)
        {
            try
            {
                using var command = client.CreateCommand(commandText);
                output = command.Execute()?.Trim() ?? string.Empty;
                return command.ExitStatus == 0;
            }
            catch
            {
                output = string.Empty;
                return false;
            }
        }
    }
}
