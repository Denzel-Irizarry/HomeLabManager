using HomeLabManager.API.Interfaces;
using HomeLabManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeLabManager.API.Infrastructure
{
    public class DeviceRepository: DeviceRepositoryInterface
    {
        private readonly ApplicationDBContext context;
        public DeviceRepository(ApplicationDBContext context)
        {
            this.context = context;
        }

        public async Task AddAsync(Device device)
        {
            await context.Devices.AddAsync(device);
        }

        public async Task<bool> SerialExistsAsynch(string serial)
        {
            return await context.Devices.AnyAsync(d => d.SerialNumber == serial);
        }
        
        //https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties?tabs=fluent-api%2Cwith-nrt
        //has more information about dbcontext and how to include related data when retrieving data from the database
        public async Task<List<Device>> GetAllAsync()
        {
            //include the related product and vendor data when retrieving devices to provide more comprehensive information about each device, including its associated product and vendor details
            return await context.Devices
            .Include(device => device.Product)
            .ThenInclude(product => product.Vendor)
            .ToListAsync();
        }

        //include the related product and vendor data when retrieving a specific device to provide more comprehensive information about the device, including its associated product and vendor details
        public async Task<Device?> GetDeviceByIdAsync(Guid id)
        {
            return await context.Devices
            .Include(device => device.Product)
            .ThenInclude(product => product.Vendor)
            .FirstOrDefaultAsync(device => device.Id == id);
        }

        public async Task<bool> DeleteByIdAsync(Guid id)
        {
            var device = await context.Devices.FindAsync(id);
            if (device == null)
            {
                return false;
            }

            context.Devices.Remove(device);
            return true;
        }
        
        public async Task<Device?> GetForUpdateByIdAsync(Guid id)
        {
            return await context.Devices
            .Include(device => device.Product)
            .ThenInclude(product => product.Vendor)
            .FirstOrDefaultAsync(device => device.Id == id);
        }

    }
}
