using System;
using System.Collections.Generic;
using System.Text;

namespace HomeLabManager.Core.Entities
{
    public class Product
    {
        //specific to each instance
        public Guid Id { get; set; }        

        public string ModelNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        //CPU information
        public int? CPUCount { get; set; }
        public string? CPUName { get; set; } = string.Empty;

        //RAM information
        public int? Memory {  get; set; }
        public int? RamSpeed { get; set; }

        //storage information
        public string? StorageForDevice { get; set; } = string.Empty;

        //able to reference different vendors
        public Vendor? Vendor { get; set; }
        //vendor specific id
        public Guid VendorId { get; set; }


    }
}
