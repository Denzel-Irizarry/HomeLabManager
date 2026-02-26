using System;
using System.Collections.Generic;
using System.Text;

namespace HomeLabManager.Core.Entities
{
    /* A device is any physical hardware I own
     * Identified by serial number 
     */
    public class Device
    {
        //Guid=(Globally Unique Identifier) => primary key for database
        public Guid Id { get; set; }
        //this will be processed for from the image scanning service
        public string? SerialNumber { get; set; } = string.Empty;

        //what to call the device could be device name or hostname
        public string? NickName { get; set; }

        //this can be set to location in RackUnits U or elsewhere such as rooms
        public string? Location { get; set; }

        //will interact with products to get this information
        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        //when device was added to interface
        public DateTime CreatedAtUtc { get; set; }=DateTime.UtcNow;
    }
}
