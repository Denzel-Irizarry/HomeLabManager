using HomeLabManager.Core.Entities;

namespace HomeLabManager.API.Interfaces
{
    public interface DeviceComponentRepositoryInterface
    {
        // Get all components installed in a specific device
        Task<IEnumerable<DeviceComponent>> GetComponentsByDeviceIdAsync(Guid deviceId);

        // Get all devices that have a specific component installed
        Task<IEnumerable<DeviceComponent>> GetDevicesByComponentIdAsync(Guid componentId);

        // Get a specific device-component installation record
        Task<DeviceComponent?> GetByIdAsync(Guid id);

        // Add a component to a device (create installation record)
        Task<DeviceComponent> AddComponentToDeviceAsync(DeviceComponent deviceComponent);

        // Update installation details (serial number, notes, etc.)
        Task<DeviceComponent?> UpdateAsync(DeviceComponent deviceComponent);

        // Remove a component from a device
        Task<bool> RemoveComponentFromDeviceAsync(Guid id);

        // Check if a specific component is already installed in a device
        Task<bool> IsComponentInstalledAsync(Guid deviceId, Guid componentId);
    }
}
