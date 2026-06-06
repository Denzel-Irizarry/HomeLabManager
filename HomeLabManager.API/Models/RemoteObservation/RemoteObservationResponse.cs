using HomeLabManager.Core.Entities;
using System;
using System.Collections.Generic;

namespace HomeLabManager.API.Models.RemoteObservation
{
    public class RemoteObservationResponse
    {
        public Guid Id { get; set; }
        public DateTime ObservedAtUtc { get; set; }
        public DeviceConnectionProfile ConnectionProfile { get; set; } = new DeviceConnectionProfile();
        public Dictionary<string, string> Facts { get; set; } = new Dictionary<string, string>();
        public string? Summary { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
