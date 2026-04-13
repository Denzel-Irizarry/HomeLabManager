using HomeLabManager.API.Services;
using HomeLabManager.Core.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeLabManager.API.Controllers
{

    [ApiController]
    [Route("api/devices/{deviceId}/components")]
    public class DeviceComponentsController : ControllerBase
    {
        private readonly DeviceComponentService deviceComponentService;

        //public way of interacting with the private service field. This allows other parts of the application to access the functionality provided by the DeviceComponentService, such as retrieving components for a device, adding components to a device,
        public DeviceComponentsController(DeviceComponentService deviceComponentService)
        {
            this.deviceComponentService = deviceComponentService;
        }

        //GET: api/devices/{deviceId}/components
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeviceComponent>>> GetComponentsByDevice(Guid deviceId)
        {
            try {
                // Call the service method to get components by device ID
                var components = await deviceComponentService.GetComponentsByDeviceIdAsync(deviceId);
                return Ok(components);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving device components.");
            }
        }

        //POST: api/devices/{deviceId}/components
        [HttpPost]
        public async Task<ActionResult<DeviceComponent>> AddComponentToDevice(Guid deviceId, [FromBody] DeviceComponent deviceComponent)
        {
            try
            {
                // Ensure the deviceId from route matches the body
                deviceComponent.DeviceId = deviceId;

                // Call the service method to add a component to the device
                var createdDeviceComponent = await deviceComponentService.AddComponentToDeviceAsync(deviceComponent);

                return CreatedAtAction(nameof(GetComponentsByDevice), new { deviceId = createdDeviceComponent.DeviceId }, createdDeviceComponent);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message); // Return bad request for validation errors
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adding the component to the device.");
            }
        }

        // DELETE: api/devices/{deviceId}/components/{componentId}
        [HttpDelete("{componentId}")]
        public async Task<ActionResult> RemoveComponentFromDevice(Guid deviceId, Guid componentId)
        {
            try
            {
                // Load the device-component record by its ID
                var record = await deviceComponentService.GetByIdAsync(componentId);
                if (record == null)
                    return NotFound($"Component record with ID {componentId} was not found.");
                

                // Verify it belongs to the device from the route
                if (record.DeviceId != deviceId)
                    return BadRequest("This component record does not belong to the specified device.");

                // 3) Safe to delete
                var deleted = await deviceComponentService.RemoveComponentFromDeviceAsync(componentId);
                if (!deleted)
                    return NotFound($"Component record with ID {componentId} was not found.");

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while removing the component from the device.");
            }
        }

    }
}
