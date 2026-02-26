using HomeLabManager.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HomeLabManager.API.ExceptionsAPI;
using HomeLabManager.Core.Entities;

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

        //posting data 
        [HttpPost("register")]
        //iaction is just being able to return information
        //IFormFile represents file over http
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

            //calls register device method that gets the scan and 
            //vendor information , and product info

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
    }
}
