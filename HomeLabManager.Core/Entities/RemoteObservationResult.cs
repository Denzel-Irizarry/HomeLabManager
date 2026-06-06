using System;
using System.Collections.Generic;

namespace HomeLabManager.Core.Entities
{
    // Read-only result model for a single remote observation probe.
    public class RemoteObservationResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Time the observation was taken
        public DateTime ObservedAtUtc { get; set; } = DateTime.UtcNow;

        // Profile used for the observation (identity, host, transport, but not secrets)
        public DeviceConnectionProfile ConnectionProfile { get; set; } = new DeviceConnectionProfile();

        // Arbitrary key/value facts discovered by the probe (e.g., os, uptime, interfaces)
        public Dictionary<string, string> Facts { get; set; } = new Dictionary<string, string>();

        // Optional textual summary
        public string? Summary { get; set; }

        // Indicator whether the probe succeeded
        public bool Success { get; set; }

        // Optional error message when Success==false
        public string? ErrorMessage { get; set; }
    }
}
