using HomeLabManager.API.ExceptionsAPI;
using HomeLabManager.API.Infrastructure;
using HomeLabManager.API.Interfaces;
using HomeLabManager.API.Models;
using HomeLabManager.Core.Entities;
using Microsoft.AspNetCore.Mvc;
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

        //this is to register a device, it takes in an image stream and returns a device object that has been created and saved to the database
        public async Task<DeviceResponseDataTransferObject> RegisterDeviceAsync(Stream imageStream)
        {
            var request = new ScanRequest
            {
                ImageStream = imageStream
            };

            //await means delay acting on the task until we get the results
            //get the serial
            var serial = await scanService.ExtractSerialAsync(request);

            //stop the process early if no serial number is found, don't start with vendor lookup or database operations
            if(string.IsNullOrWhiteSpace(serial))
            {
                throw new SerialNumberMissingException("Serial number could not be extracted from the image.");
            }

            //check for duplicate serial number before checking with vendor lookup or database operations and stops device from being created
            var existingDevice = await deviceRepository.SerialExistsAsynch(serial);
            if(existingDevice)
            {
                throw new DuplicateSerialNumberException("A device with the same serial number already exists.");
            }

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
                CreatedAtUtc = DateTime.UtcNow
            };

            //adds the created device so it can persist
            await deviceRepository.AddAsync(device);

            //this is will send the save request
            await dbContext.SaveChangesAsync();

           
            return MapToDataTransfer(device);

        }

        //get all devices from database and return them to the caller
        public async Task<List<DeviceResponseDataTransferObject>> GetAllDevicesAsync()
        {
            var devices = await deviceRepository.GetAllAsync();
            return devices.Select(MapToDataTransfer).ToList();
        }

        //get a specific device by id from database and return it to the caller
        public async Task<DeviceResponseDataTransferObject?> GetDeviceByIdAsync(Guid id)
        {
            var device = await deviceRepository.GetDeviceByIdAsync(id);
            if(device == null)
                return null;

            return MapToDataTransfer(device);
        }

        //delete a specific device by id from database and return a boolean indicating success or failure
        public async Task<bool> DeleteDeviceByIdAsync(Guid id)
        {
            var deleted = await deviceRepository.DeleteByIdAsync(id);
            if(deleted)
            {
                //this will let the ui know the delete was unsuccessful and to update the list of devices
                return false;
            }

            await dbContext.SaveChangesAsync();

            return true;
        }

        //https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.datatransfer?view=winrt-26100
        private static DeviceResponseDataTransferObject MapToDataTransfer(Device device)
        {
            return new DeviceResponseDataTransferObject
            {
                Id=device.Id,
                SerialNumber=device.SerialNumber,
                NickName = device.NickName,
                Location = device.Location,
                CreatedAtUtc = device.CreatedAtUtc,
                ProductName = device.Product?.ProductName,//get device & product relationship and extract product name
                ModelNumber = device.Product?.ModelNumber,//get device & product relationship and extract model number
                VendorName = device.Product?.Vendor?.VendorName//get device & product relationship, get product & vendor relationship, get vendor name

            };
        }
    
        public async Task<DeviceResponseDataTransferObject> RegisterManualDeviceAsync(ManualDeviceRegisterRequest request)
        {
            if(string.IsNullOrWhiteSpace(request.SerialNumber) && string.IsNullOrWhiteSpace(request.NickName))
                throw new SerialNumberMissingException("Provide at least SerialNumber or NickName");

            if (!string.IsNullOrWhiteSpace(request.SerialNumber))
            {
                var existsAlready = await deviceRepository.SerialExistsAsynch(request.SerialNumber);
                if(existsAlready)
                    throw new DuplicateSerialNumberException("A device with the same serial number already exists.");
            }

            //sets the vendor for the manual entry
            var vendor = new Vendor
            {
                Id = Guid.NewGuid(),
                VendorName = request.VendorName ?? "ManualEntry"//returns this if left null
            };

            //sets the product info for manual entry
            var product = new Product
            {
                Id = Guid.NewGuid(),
                ProductName = request.ProductName ?? "Unkown Product",//returns this if left null
                ModelNumber = request.ModelNumber ?? "Unknown Model",
                VendorId = vendor.Id,
                Vendor = vendor
            };
            
            //sets the device info for manual entry
            var device = new Device
            {
                Id = Guid.NewGuid(),
                SerialNumber = request.SerialNumber,
                NickName = request.NickName,
                Location = request.Location,
                ProductId = product.Id,
                Product = product,
                CreatedAtUtc = DateTime.UtcNow
            };

          //adds the created device so it can persist
            await deviceRepository.AddAsync(device);

            //this is will send the save request
            await dbContext.SaveChangesAsync();

            return MapToDataTransfer(device);

        }


    }
}
