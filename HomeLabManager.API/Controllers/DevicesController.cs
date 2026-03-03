using HomeLabManager.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HomeLabManager.API.ExceptionsAPI;
using HomeLabManager.Core.Entities;
using HomeLabManager.API.Models;

namespace HomeLabManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        //initialize instance of Device Service
        private readonly DeviceService deviceService;

        //ILogger is used for logging information, warnings, and errors in the application, helps with debugging and monitoring
        //https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger?view=net-10.0-pp

        private readonly ILogger<DevicesController> logger;

        //how to actually call the device controller
        public DevicesController(DeviceService deviceService, ILogger<DevicesController> logger)
        {
            this.deviceService = deviceService;
            this.logger = logger;
        }

        // post request to register a device, it takes in an image file and returns a device object that has been created and saved to the database       
        // iaction is just being able to return information
        // IFormFile represents file over http
        [HttpPost("register")]
        public async Task<IActionResult> Register(IFormFile file)
        {
            try{


            if(file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }


            //opens a request to do the actual reading of the file
            //Passing the IFormFile to a stream because stream is .Net compatible with any framework
            //will work in asp.net or blazor a.k.a. translating to a universal type
            using var stream = file.OpenReadStream();

            //return the device created from the deviceService class
            var device = await deviceService.RegisterDeviceAsync(stream);
            
            return Ok(device);
            }
            catch(SerialNumberMissingException ex)
            {
                logger.LogWarning(ex, "Serial number missing during device registration.");
                return BadRequest(ex.Message);
            }
            catch(DuplicateSerialNumberException ex)
            {
                logger.LogWarning(ex, "Duplicate serial number during device registration.");
                return Conflict(ex.Message);
            }
            catch(FileScanningUploadException ex)
            {
                logger.LogWarning(ex, "File scanning failed during device registration.");
                //early exit if empty or invalid file, no need to continue processing
                return BadRequest(ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, $"An error occurred while processing the request.");
            }

        }
        
        // post request to manual register a device     
        // iaction is just being able to return information
        // IFormFile represents file over http
        [HttpPost("manual")]
        public async Task<IActionResult> RegisterManual([FromBody] ManualDeviceRegisterRequest registerRequest)
        {
            try
            {
                //device created from the deviceService class
                var device = await deviceService.RegisterManualDeviceAsync(registerRequest);
                //return the device created from the deviceService class
                return Ok(device);

            }
                        catch(SerialNumberMissingException ex)
            {
                logger.LogWarning(ex, "Serial number missing during device registration.");
                return BadRequest(ex.Message);
            }
            catch(DuplicateSerialNumberException ex)
            {
                logger.LogWarning(ex, "Duplicate serial number during device registration.");
                return Conflict(ex.Message);
            }
            catch(FileScanningUploadException ex)
            {
                logger.LogWarning(ex, "File scanning failed during device registration.");
                //early exit if empty or invalid file, no need to continue processing
                return BadRequest(ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing the request.");
                return StatusCode(500, $"An error occurred while processing the request.");
            }
        }

        //needs exception handling for if there is an error during the retrieval process
        //get request to get all devices in the system, including related product and vendor information
        [HttpGet]
        public async Task<ActionResult<List<Device>>> GetDevices()
        {
            var devices = await deviceService.GetAllDevicesAsync();
            return Ok(devices);
        }

        //needs exception handling for if device with id is not found
        //get request to get a specific device by id, including related product and vendor information
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Device>> GetDeviceById(Guid id)
        {
            var device = await deviceService.GetDeviceByIdAsync(id);
            if(device == null)
            {
                return NotFound();
            }
            return Ok(device);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDeviceStats()
        {
            try
            {
                var stats = await deviceService.GetDeviceStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving device statistics.");
                return StatusCode(500, "An error occurred while retrieving device statistics.");
            }
        }

        //needs exception handling for if device with id is not found, or if there is an error during the delete process
        //delete request to delete a specific device by id
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteDevice(Guid id)
        {
            var deleted = await deviceService.DeleteDeviceByIdAsync(id);
            if(!deleted)
            {
                //404 means the device with the specified id was not found in the database, so it cannot be deleted
                return NotFound();
            }

            //204 means the request was successful but there is no content to return because the device was deleted
            return NoContent();
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateDevice(Guid id, [FromBody] UpdateDeviceRequest request)
        {
            try
            {
                var device = await deviceService.UpdateDeviceAsync(id, request);
                if (device == null)
                {
                    return NotFound($"Device with id '{id}' was not found.");
                }
                return Ok(device);
            }
            catch (SerialNumberMissingException ex)
            {
                logger.LogWarning(ex, "Serial number missing during device update.");
                return BadRequest(ex.Message);
            }
            catch (DuplicateSerialNumberException ex)
            {
                logger.LogWarning(ex, "Duplicate serial number during device update.");
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating the device.");
                return StatusCode(500, "An error occurred while updating the device.");
            }
        }


        
    }
}
