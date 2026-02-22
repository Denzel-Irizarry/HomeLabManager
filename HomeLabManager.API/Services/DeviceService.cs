using HomeLabManager.API.Interfaces;
using HomeLabManager.API.Models;
using HomeLabManager.Core.Entities;


namespace HomeLabManager.API.Services
{
    public class DeviceService
    {
        //initiateing the scan, vendor, device interfaces for use with deviceservice
        private readonly ScanServiceInterface scanService;
        private readonly VendorLookupInterface vendorLookup;
        private readonly DeviceRepositoryInterface deviceRepository;

        public DeviceService(ScanServiceInterface scanService, VendorLookupInterface vendorLookup, DeviceRepositoryInterface deviceRepository)
        {
            this.scanService = scanService;
            this.vendorLookup = vendorLookup;
            this.deviceRepository = deviceRepository;
        }

        //helpful info about async to reference 
        // https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/
        //task is represents the work being done for async processes 
        public async Task<Device> RegisterDeviceAsync(Stream imageStream)
        {
            var request = new ScanRequest
            {
                ImageStream = imageStream
            };

            //await means delay acting on the task until we get the results
            //get the serial
            var serial = await scanService.ExtractSerialAsync(request);

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

            //adds the created device so it can persist
            await deviceRepository.AddAsync(device);

            //this is where i want to send it to the DB not set up yet ///////////
            return device;

        }

    }
}
