using System;
using System.Collections.Generic;
using System.Text;

namespace HomeLabManager.Core.Entities
{
    public class DeviceComponent
    {
        public Guid Id { get; set; }

        //foreign key to link devices and components
        public Guid DeviceId { get; set; }
        public Guid ComponentId { get; set; }

        //serial number of the component, if applicable. This can be used to track the specific component and its history, such as when it was installed, when it was removed, and any issues or repairs that may have occurred with it.
        public string? SerialNumber { get; set; }

        //when the component was installed in the device. This can be used to track the age of the component and when it may need to be replaced or upgraded.
        public DateTime? InstalledDate {  get; set; }

        //installation notes such as "replaced old hard drive with new one" or "added 2 RAM sticks for upgrade". This can be used to track the history of the device and the changes made to it over time.
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }=DateTime.UtcNow;

    }
}
