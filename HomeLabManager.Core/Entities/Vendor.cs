using System;
using System.Collections.Generic;
using System.Text;

namespace HomeLabManager.Core.Entities
{
    public class Vendor
    {
        //id specific to vendor
        public Guid Id { get; set; }

        public string VendorName { get; set; } = string.Empty;



    }
}
