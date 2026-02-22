using HomeLabManager.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeLabManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        //initialize instance of Device Service
        private readonly DeviceService deviceService;

        //how to actually call the device controller
        public DevicesController(DeviceService deviceService)
        {
            this.deviceService = deviceService;
        }

        //posting data 
        [HttpPost("register")]
        //iaction is just being able to return information
        //IFormFile represents file over http
        public async Task<IActionResult> Register(IFormFile file)
        {
            //early exit if empty
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
            var device = await deviceService.RegisterDeviceAsync(stream);

            //return the device created from the deviceService class
            return Ok(device);
        }
    }
}
