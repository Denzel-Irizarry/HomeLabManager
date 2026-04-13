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
            // serial extracted

            //stop the process early if no serial number is found, don't start with vendor lookup or database operations
            if (string.IsNullOrWhiteSpace(serial))
            {
                throw new SerialNumberMissingException("Serial number could not be extracted from the image.");
            }

            //check for duplicate serial number before checking with vendor lookup or database operations and stops device from being created
            var existingDevice = await deviceRepository.SerialExistsAsynch(serial);
            // duplicate check result
            if (existingDevice)
            {
                throw new DuplicateSerialNumberException("A device with the same serial number already exists.");
            }

            //await means delay acting on the task until we get the results
            //lookup product from vendor
            var product = await vendorLookup.GetProductBySerialAsync(serial);
            // vendor lookup completed

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
            // device added to DbContext

            //this is will send the save request
            await dbContext.SaveChangesAsync();
            // SaveChangesAsync completed


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
            if (device == null)
                return null;

            return MapToDataTransfer(device);
        }

        //delete a specific device by id from database and return a boolean indicating success or failure
        public async Task<bool> DeleteDeviceByIdAsync(Guid id)
        {
            var deleted = await deviceRepository.DeleteByIdAsync(id);
            if (!deleted)
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
                Id = device.Id,
                SerialNumber = device.SerialNumber,
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
            if (string.IsNullOrWhiteSpace(request.SerialNumber) && string.IsNullOrWhiteSpace(request.NickName))
                throw new SerialNumberMissingException("Provide at least SerialNumber or NickName");

            if (!string.IsNullOrWhiteSpace(request.SerialNumber))
            {
                var existsAlready = await deviceRepository.SerialExistsAsynch(request.SerialNumber);
                if (existsAlready)
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

        public async Task<DeviceResponseDataTransferObject?> UpdateDeviceAsync(Guid id, UpdateDeviceRequest request)
        {
            //trim the incoming data to remove any leading or trailing whitespace, this will help with consistency and prevent issues with duplicate serial numbers that are actually the same but have extra spaces
            var serial = request.SerialNumber?.Trim();
            var nickName = request.NickName?.Trim();
            var location = request.Location?.Trim();
            var productName = request.ProductName?.Trim();
            var modelNumber = request.ModelNumber?.Trim();
            var vendorName = request.VendorName?.Trim();

            //get the device for update, this will track the entity and allow us to make changes and then call save changes to persist those changes
            var device = await deviceRepository.GetForUpdateByIdAsync(id);

            //if the device doesn't exist return null so the ui can handle it and not found response
            if (device == null)
                return null;

            //if the serial number is being updated, check for duplicates before updating and saving, this will stop the update and
            //return an error to the user if they try to update to a serial number that already exists in the system
            if (!string.IsNullOrWhiteSpace(serial) && serial != device.SerialNumber)
            {
                var existsAlready = await deviceRepository.SerialExistsAsynch(serial);
                if (existsAlready)
                    throw new DuplicateSerialNumberException("A device with the same serial number already exists.");
                device.SerialNumber = serial;
            }
            else if(serial is not null)
            {
                //allow explicit clearing of the serial number if the user is trying to remove it, but only if they have a nickname so we don't end up with a device that has no way to identify it
                device.SerialNumber = serial;
            }
            //update the other fields if they are not null, this allows for partial updates and doesn't require the user to send all the data if they only want to update one or two fields
            if (nickName is not null) { device.NickName = nickName;}
            if (location is not null) { device.Location = location;}

            if (device.Product != null)
            {
                //if the product name or model number is being updated, we need to update the product entity as well, this will allow us to keep the relationships intact and not end up with orphaned entities or inconsistent data
                if (productName is not null) { device.Product.ProductName = productName; }
                if (modelNumber is not null) { device.Product.ModelNumber = modelNumber; }

                //if the vendor name is being updated, we need to update the vendor entity as well, this will allow us to keep the relationships intact and not end up with orphaned entities or inconsistent data
                if(device.Product.Vendor != null && vendorName is not null) 
                { 
                    device.Product.Vendor.VendorName = vendorName; 
                }
            }

            //if the user is trying to update the device and they are removing the serial number, make sure they have a nickname so we don't end up with a device that has no way to identify it
            if (string.IsNullOrWhiteSpace(device.SerialNumber) && string.IsNullOrWhiteSpace(device.NickName))
            {
                throw new SerialNumberMissingException("Provide at least SerialNumber or NickName");
            }


            //this is will send the save request and persist the changes to the database
            await dbContext.SaveChangesAsync();
            return MapToDataTransfer(device);

        }
        public async Task<DeviceStatsResponse> GetDeviceStatsAsync()
        {
            //gets the total number of devices in the system, this is a simple count query that will be fast and efficient even with a large number of devices, this will allow us to provide some basic stats to the user about their device inventory
            var totalDevices = await dbContext.Devices.CountAsync();

            //gets the number of devices that have a serial number, this is a count query with a filter
            //that will count only the devices that have a non-empty serial number
            var withSerialNumber = await dbContext.Devices.CountAsync(device => !string.IsNullOrEmpty(device.SerialNumber));

            //gets the number of devices that do not have a serial number, this is a count query with a filter
            //that will count only the devices that have an empty or null serial number
            var withoutSerialNumber = await dbContext.Devices.CountAsync(device => string.IsNullOrEmpty(device.SerialNumber));

            //gets the number of devices that have a nickname, this is a count query with a filter
            //that will count only the devices that have a non-empty nickname
            var withNickName = await dbContext.Devices.CountAsync(device => !string.IsNullOrEmpty(device.NickName));


            //gets the number of devices that do not have a nickname, this is a count query with a filter
            //that will count only the devices that have an empty or null nickname
            var withoutNickName = await dbContext.Devices.CountAsync(device => string.IsNullOrEmpty(device.NickName));

            //gets the number of devices that have a location, this is a count query with a filter
            //that will count only the devices that have a non-empty location
            var withLocation = await dbContext.Devices.CountAsync(device => !string.IsNullOrEmpty(device.Location));

            //gets the number of devices that do not have a location, this is a count query with a filter
            //that will count only the devices that have an empty or null location
            var withoutLocation = await dbContext.Devices.CountAsync(device => string.IsNullOrEmpty(device.Location));

            //gets the most recent device added to the system, this is a query that orders the devices by created date and takes the first one
            var lastAddedUtc = await dbContext.Devices.OrderByDescending(d => d.CreatedAtUtc).Select(d => (DateTime?)d.CreatedAtUtc).FirstOrDefaultAsync();

            return new DeviceStatsResponse
            {
                TotalDevices = totalDevices,
                WithSerialNumber = withSerialNumber,
                WithoutSerialNumber = withoutSerialNumber,
                WithNickName = withNickName,
                WithoutNickName = withoutNickName,
                WithLocation = withLocation,
                WithoutLocation = withoutLocation,
                LastAddedUtc = lastAddedUtc
            };
        }
    }
}
