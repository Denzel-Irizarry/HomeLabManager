using System;

namespace HomeLabManager.Core.Entities
{
    // Transport-agnostic connection profile for remote observation.
    public class DeviceConnectionProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Hostname or IP address of the target device
        public string Host { get; set; } = string.Empty;

        // Optional port (e.g., 22 for SSH)
        public int? Port { get; set; }

        // Transport type, e.g. "ssh" or "agent". Keep as string for forward-compat.
        public string Transport { get; set; } = "ssh";

        // Reference to credential material stored externally (secret store key, vault id, etc.)
        // IMPORTANT: Do NOT store plaintext secrets here.
        public string CredentialReference { get; set; } = string.Empty;

        // Optional user or identity name to use for authentication (not a secret)
        public string? IdentityName { get; set; }
    }
}
