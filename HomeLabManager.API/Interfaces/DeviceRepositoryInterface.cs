using HomeLabManager.Core.Entities;

namespace HomeLabManager.API.Interfaces
{
    public interface DeviceRepositoryInterface
    {
        //to save device
        Task AddAsync(Device device);
        //check if it already is in system 
        Task<bool> SerialExistsAsynch(string serial);

        //get all devices in the system, including related product and vendor information
        Task<List<Device>> GetAllAsync();

        //get a specific device by id, including related product and vendor information
        Task<Device?> GetDeviceByIdAsync(Guid id);

        //get a specific device by id for update, including related product and vendor information
        Task<Device?> GetForUpdateByIdAsync(Guid id);

        //delete a specific device by id
        Task<bool> DeleteByIdAsync(Guid id);


    }
}
