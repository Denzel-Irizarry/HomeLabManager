using HomeLabManager.API.Interfaces;
using HomeLabManager.Core.Entities;
using Microsoft.EntityFrameworkCore;


namespace HomeLabManager.API.Infrastructure
{
    public class DeviceComponentRepository:DeviceComponentRepositoryInterface
    {
        // Repository for managing the many-to-many relationship between devices and components
        private readonly ApplicationDBContext applicationDBContext;

        public DeviceComponentRepository(ApplicationDBContext applicationDBContext)
        {
            this.applicationDBContext = applicationDBContext;
        }


        public async Task<IEnumerable<DeviceComponent>> GetComponentsByDeviceIdAsync(Guid deviceId)
        {
            // Get all components installed in a specific device from the DeviceComponents table
            return await applicationDBContext.DeviceComponents
                .Where(dc => dc.DeviceId == deviceId) // Filter by device ID
                .ToListAsync(); // Execute the query and return the results as a list
        }

        public async Task<IEnumerable<DeviceComponent>> GetDevicesByComponentIdAsync(Guid componentId)
        {
            //get all devices that have a specific component installed from the DeviceComponents table
            return await applicationDBContext.DeviceComponents
                .Where(dc => dc.ComponentId == componentId) // Filter by component ID
                .ToListAsync(); // Execute the query and return the results as a list
        }

        public async Task<DeviceComponent?> GetByIdAsync(Guid id)
        {
            return await applicationDBContext.DeviceComponents
                .FirstOrDefaultAsync(dc => dc.Id == id); // Find the device-component relationship by its unique ID
        }

        public async Task<DeviceComponent> AddComponentToDeviceAsync(DeviceComponent deviceComponent)
        {
            //Generate new ID id not provided
            if (deviceComponent.Id == Guid.Empty) deviceComponent.Id = Guid.NewGuid();

            // Add a new device-component relationship to the DeviceComponents table
            applicationDBContext.DeviceComponents.Add(deviceComponent); // Add the new relationship to the DbSet

            await applicationDBContext.SaveChangesAsync(); // Save changes to the database
            return deviceComponent; // Return the added relationship
        }

        public async Task<DeviceComponent?> UpdateAsync(DeviceComponent deviceComponent) 
        {
            //find the existing record in the database
            var existingRecord = await applicationDBContext.DeviceComponents.FindAsync(deviceComponent.Id);
            
            if(existingRecord == null) return null; // Return null if the record does not exist

            // Update the existing record with new values
            existingRecord.SerialNumber = deviceComponent.SerialNumber;
            existingRecord.InstalledDate = deviceComponent.InstalledDate;
            existingRecord.Notes = deviceComponent.Notes;

            //actually save the chages to the database
            await applicationDBContext.SaveChangesAsync();
            return existingRecord; // Return the updated device information
        }

        public async Task<bool> RemoveComponentFromDeviceAsync(Guid id)
        {
            // Find the device-component relationship by its unique ID
            var deviceComponent = await applicationDBContext.DeviceComponents.FindAsync(id); 
            if(deviceComponent == null) return false; // Return false if the relationship does not exist

            // Remove the relationship from the DbSet
            applicationDBContext.DeviceComponents.Remove(deviceComponent);
            // Save changes to the database
            await applicationDBContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsComponentInstalledAsync(Guid deviceId, Guid componentId)
        {
            // Check if a specific component is already installed in a device by querying the DeviceComponents table
            // Return true if a matching record exists, otherwise false
            return await applicationDBContext.DeviceComponents
                .AnyAsync(dc => dc.DeviceId == deviceId && dc.ComponentId == componentId); 
        }



    }
}
