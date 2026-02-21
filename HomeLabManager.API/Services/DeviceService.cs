using HomeLabManager.API.Interfaces;
using HomeLabManager.Core.Entities;


namespace HomeLabManager.API.Services
{
    public class DeviceService
    {
        //initiateing the scan and vendor interaces for use with deviceservice
        private readonly ScanServiceInterface scanService;
        private readonly VendorLookupInterface vendorLookup;

        public DeviceService(ScanServiceInterface scanService, VendorLookupInterface vendorLookup)
        {
            this.scanService = scanService;
            this.vendorLookup = vendorLookup;
        }

        //helpful info about async to reference 
        // https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/
        //task is represents the work being done for async processes 
        public async Task<Device> RegisterDeviceAsync(Stream imageStream)
        {
            //await means delay acting on the task until we get the results
            //get the serial
            var serial = await scanService.ExtractSerialAsync(imageStream);

            //await means delay acting on the task until we get the results
            //lookup product from vendor
            var product = await vendorLookup.GetProductBySerialAsync(serial);

            //create the actual device
            var device = new Device
            {
                Id = Guid.NewGuid(),
                SerialNumber = serial,
                ProductId = product.Id,
                Product = product,
                CreatedAt = DateTime.Now
            };

            //this is where i want to send it to the DB not set up yet ///////////
            return device;

        }

    }
}
