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
        

    }
}
