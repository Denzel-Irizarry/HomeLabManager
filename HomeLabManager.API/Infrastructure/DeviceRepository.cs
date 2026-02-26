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

    }
}
