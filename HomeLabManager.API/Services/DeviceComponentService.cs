using HomeLabManager.API.Interfaces;
using HomeLabManager.Core.Entities;

namespace HomeLabManager.API.Services
{
    public class DeviceComponentService
    {
        private readonly DeviceComponentRepositoryInterface deviceComponentRepository;
        private readonly DeviceRepositoryInterface deviceRepository;
        private readonly ComponentRepositoryInterface componentRepository;

        // DeviceComponentService class takes instances of the three repositories as parameters. This allows the service to interact with the data repositories for devices, components, and their relationships, enabling it to perform various operations related to managing the associations between devices and components in the database.
        public DeviceComponentService(DeviceComponentRepositoryInterface deviceComponentRepository, DeviceRepositoryInterface deviceRepository, ComponentRepositoryInterface componentRepository)
        {
            this.deviceComponentRepository = deviceComponentRepository;
            this.deviceRepository = deviceRepository;
            this.componentRepository = componentRepository;
        }

        public async Task<IEnumerable<DeviceComponent>> GetComponentsByDeviceIdAsync(Guid deviceId)
        {
            // This method retrieves a list of DeviceComponent entities associated with a specific device ID. It uses the deviceComponentRepository to query the database and return the relevant components for the given device.
            return await deviceComponentRepository.GetComponentsByDeviceIdAsync(deviceId);
        }

        public async Task<IEnumerable<DeviceComponent>> GetDevicesByComponentIdAsync(Guid componentId)
        {
           // This method retrieves a list of DeviceComponent entities associated with a specific component ID. It uses the deviceComponentRepository to query the database and return the relevant devices that have the specified component installed.
            return await deviceComponentRepository.GetDevicesByComponentIdAsync(componentId);
        }
        public async Task<DeviceComponent?> GetByIdAsync(Guid id)
        {
            // retrieves a specific DeviceComponent by its unique ID. It uses the deviceComponentRepository to query the database and return the corresponding device-component association if it exists.
            return await deviceComponentRepository.GetByIdAsync(id);
        }
        public async Task<DeviceComponent?> AddComponentToDeviceAsync(DeviceComponent deviceComponent)
        {
            // Adds a new association between a device and a component. It takes a DeviceComponent object as input, which contains the details of the association (such as device ID, component ID, serial number, etc.). The method then uses the deviceComponentRepository to save this new association to the database and returns the created DeviceComponent.

            //check to see if device exists
            var device = await deviceRepository.GetDeviceByIdAsync(deviceComponent.DeviceId);
            if(device == null)
                throw new ArgumentException($"Device with ID {deviceComponent.DeviceId} does not exist.");

            //check to see if component exists
            var component = await componentRepository.GetByIdAsync(deviceComponent.ComponentId);
            if(component == null)
                throw new ArgumentException($"Component with ID {deviceComponent.ComponentId} does not exist.");

            // generate new guid if not provided
            if (deviceComponent.Id == Guid.Empty)
                deviceComponent.Id = Guid.NewGuid();

            //set installed date to now if not provided
            if (deviceComponent.InstalledDate == null)
                deviceComponent.InstalledDate = DateTime.UtcNow;

            return await deviceComponentRepository.AddComponentToDeviceAsync(deviceComponent);
        }
        public async Task<DeviceComponent?> UpdateAsync(DeviceComponent deviceComponent)
        {
            // Updates installation details like serial number, install date, notes
            if (deviceComponent.Id == Guid.Empty)
                throw new ArgumentException("DeviceComponent ID is required for update.");

            return await deviceComponentRepository.UpdateAsync(deviceComponent);
        }

        public async Task<bool> RemoveComponentFromDeviceAsync(Guid id)
        {
            // Removes a component installation from a device
            if (id == Guid.Empty)
                throw new ArgumentException("Valid DeviceComponent ID is required.");

            return await deviceComponentRepository.RemoveComponentFromDeviceAsync(id);
        }
    }
}
