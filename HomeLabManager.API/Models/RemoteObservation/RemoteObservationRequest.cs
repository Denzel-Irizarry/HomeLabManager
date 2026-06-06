using HomeLabManager.Core.Entities;
using System;

namespace HomeLabManager.API.Models.RemoteObservation
{
    // API-side request that references either an existing device id or an inline connection profile.
    public class RemoteObservationRequest
    {
        // Optional: probe using an existing connection profile id (future)
        public Guid? ConnectionProfileId { get; set; }

        // Optional inline profile to use for this probe. Secrets must not be included here.
        public DeviceConnectionProfile? InlineProfile { get; set; }
    }
}
