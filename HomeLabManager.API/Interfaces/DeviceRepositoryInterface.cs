using HomeLabManager.Core.Entities;

namespace HomeLabManager.API.Interfaces
{
    public interface DeviceRepositoryInterface
    {
        //to save device
        Task AddAsync(Device device);
        //check if it already is in system 
        Task<bool> SerialExistsAsynch(string serial);

        Task<List<Device>> GetAllAsync();

    }
}
