using HomeLabManager.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HomeLabManager.API.Services;

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
        public async Task<IActionResult> Register()
        {
            //fake test steam
            using var stream = new MemoryStream();

            //calls register device method that gets the scan and 
            //vendor information , and product info
            var device = await deviceService.RegisterDeviceAsync(stream);

            //return the device created from the deviceService class
            return Ok(device);
        }
    }
}
