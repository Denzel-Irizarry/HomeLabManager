using System;
using System.Collections.Generic;
using System.Text;

namespace HomeLabManager.Core.Entities
{
    /* A component is any part that goes into a device, such as a CPU, RAM, or hard drive. 
     * It can be used to track the individual parts of a device and their specifications.
     * It can also be used to track the history of a device, such as when a component was added or removed.
     */
    public class Component
    {
        public Guid Id { get; set; }
        public string Name { get; set; }= string.Empty;

        /*type of component such as CPU, RAM, or hard drive. This can be used to categorize components and filter them based on their type. It can also be used to track the specifications of the component, such as the model number, manufacturer, and other relevant details.
         */
        public string? ComponentType { get; set; }
        public string? Manufacturer { get; set; }
        public string? ModelNumber { get; set; }
        //specifications of the component such as clock speed for CPU, capacity for hard drive, and size for RAM. This can be used to compare components and determine their performance and compatibility with other components.
        public string? Specifications { get; set; }
        public decimal? UnitPrice { get; set; }

        // Optional: Link to vendor who supplies this component
        public Guid? VendorId { get; set; }

        //when the device was added to interface
        public DateTime CreatedAtUtc { get; set; }=DateTime.UtcNow;

    }
}
