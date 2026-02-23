using HomeLabManager.API.Infrastructure;
using HomeLabManager.API.Interfaces;
using HomeLabManager.API.Models;
using HomeLabManager.Core.Entities;
using Microsoft.EntityFrameworkCore;


namespace HomeLabManager.API.Services
{
    public class DeviceService
    {
        //initiateing the dependencies for the device service, these are the services that will be used to complete the process of registering a device
        private readonly ScanServiceInterface scanService;
        private readonly VendorLookupInterface vendorLookup;
        private readonly DeviceRepositoryInterface deviceRepository;
        private readonly ApplicationDBContext dbContext;

        public DeviceService(
            ScanServiceInterface scanService, 
            VendorLookupInterface vendorLookup, 
            DeviceRepositoryInterface deviceRepository, 
            ApplicationDBContext dbContext)
        {
            this.scanService = scanService;
            this.vendorLookup = vendorLookup;
            this.deviceRepository = deviceRepository;
            this.dbContext = dbContext;
            
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

            //this is where it actually saves it once it has been added
            await dbContext.SaveChangesAsync();

            //this is where i want to send it to the DB not set up yet ///////////
            return device;

        }

    }
}
